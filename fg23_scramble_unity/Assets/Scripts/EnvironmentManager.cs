using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using TMPro;
using UnityEngine.UIElements;
using System.Linq;
using UnityEditor.Search;


#if UNITY_EDITOR
using UnityEditor;
#endif

 [System.Serializable]
public enum EControlState
{
    NONE = 0,
    STOP = 1,
    SCRAMBLE = 2,
    REVERT = 3
}

[ExecuteInEditMode]
public class EnvironmentManager : MonoBehaviour
{
    [SerializeField] private List<EnvironmentInstance> m_environmentInstances;                

    [SerializeField] private Vector3 m_randomZone = new Vector3(10, 10, 10);
    [SerializeField] private Vector2 m_randomRotationRange = new Vector2(-1.0f, 1.0f);
    [SerializeField] private Vector2 m_randomScaleRange = new Vector2(0.2f, 2.0f);
    [SerializeField] private float m_totalTravelTime = 5; // Time in Seconds

    [SerializeField] private EControlState m_currentState;

    public List<EnvironmentInstance> EnvironmentInstances
    {
        get { return m_environmentInstances; }
    }    

    public Vector3 RandomZone { get { return m_randomZone; } }
    public Vector2 RandomRotationRange { get { return m_randomRotationRange; } }
    public Vector3 RandomScaleRange { get { return m_randomScaleRange; } }

    public float TotalTravelTime { get { return m_totalTravelTime; } }

    public EControlState CurrentState 
    { 
        get 
        { 
            return m_currentState; 
        } 
        set 
        {            
            switch (value)
            {
                case EControlState.SCRAMBLE:                    
                    RandomiseTarget();
                    break;
                case EControlState.STOP:
                    foreach(var ei in m_environmentInstances)
                    {
                        ei.Realign(EnvironmentInstance.EMoveType.TARGET);
                    } 
                    break;
                case EControlState.NONE:
                    foreach(var ei in m_environmentInstances)
                    {
                        ei.Realign(EnvironmentInstance.EMoveType.INITIAL);
                    } 
                    break;
            } 
            m_currentState = value; 
        } 
    }

    void OnEnable()
    {
        if (m_environmentInstances == null) m_environmentInstances = new List<EnvironmentInstance>();
        Populate(transform, 0);
        SortEnvironmentInstances();
        foreach(var ei in m_environmentInstances)
        {
            ei.Initialize(this);
        }                
    }    

    void FixedUpdate()
    {
        if (Application.isPlaying)
        {
            var stoppedItems = 0;
            switch(CurrentState)
            {
                case EControlState.NONE:                    
                    break;
                case EControlState.STOP:                    
                    break;
                case EControlState.SCRAMBLE:                                           
                    foreach(var ei in m_environmentInstances)
                    {                    
                        // given current time and speed, find correct position                                                                                                     
                        var dir = (ei.TargetWorldPosition - ei.StartWorldPosition).normalized;
                        var speed = (ei.TargetWorldPosition - ei.StartWorldPosition).magnitude / TotalTravelTime;
                        var vel = dir * speed * Time.fixedDeltaTime;
                        var previous = Vector3Util.InverseLerp(ei.StartWorldPosition, ei.TargetWorldPosition, ei.transform.position);                                            
                        var next = Vector3Util.InverseLerp(ei.StartWorldPosition, ei.TargetWorldPosition, ei.transform.position + vel);                
                        
                        if (next > 1.0f) { stoppedItems++; continue; }

                        ei.Move(next - previous, EnvironmentInstance.EMoveType.TARGET);
                    }                                   
                    if (stoppedItems >= m_environmentInstances.Count) CurrentState = EControlState.STOP;
                    break;
                case EControlState.REVERT:                    
                    foreach(var ei in m_environmentInstances)
                    {                    
                        // given current time and speed, find correct position                                                                                                     
                        var dir = (ei.InitialWorldPosition - ei.TargetWorldPosition).normalized;
                        var speed = (ei.TargetWorldPosition - ei.InitialWorldPosition).magnitude / TotalTravelTime;
                        var vel = dir * speed * Time.fixedDeltaTime;
                        var previous = Vector3Util.InverseLerp(ei.TargetWorldPosition, ei.InitialWorldPosition, ei.transform.position);                                            
                        var next = Vector3Util.InverseLerp(ei.TargetWorldPosition, ei.InitialWorldPosition, ei.transform.position + vel);

                        if (next > 1.0f) { stoppedItems++; continue; }

                        ei.Move(next - previous, EnvironmentInstance.EMoveType.INITIAL);
                    }                              
                    if (stoppedItems >= m_environmentInstances.Count) CurrentState = EControlState.NONE;
                    break;
            }        
        }
    }

    void OnTransformChildrenChanged()
    {
        Populate(transform, 0);
        SortEnvironmentInstances();
    }

    public void Populate(Transform t, int priority)
    {
        // TODO: make it more efficient by only adding the "new" item inside. don't have to loop through all child objects
        for(int i=0; i<t.childCount; i++)
        {
            var childObj = t.GetChild(i);            
            var envInstance = childObj.GetComponent<EnvironmentInstance>();            
            
            if (envInstance == null) 
            { 
                envInstance = childObj.AddComponent<EnvironmentInstance>();            
                envInstance.Initialize(this, priority);
            }
            else
            {
                envInstance.Priority = priority;
            }

            var isDuplicate = m_environmentInstances.FindAll( x=> x.ID == envInstance.ID);            

            if (isDuplicate.Count == 0 || isDuplicate == null)
            {
                // Debug.Log("duplicate null or 0");
                m_environmentInstances.Add(envInstance);                
            }
            else if (isDuplicate.Count >= 1)
            {                
                foreach (var d in isDuplicate)
                {
                    if (d != envInstance)
                    {
                        // Debug.Log("duplicate count >= 1");
                        envInstance.Initialize(this, priority, true);
                        m_environmentInstances.Add(envInstance);
                    }
                }
            }                        

            // TOOD: may need to recursively add objects
            Populate(childObj, priority+1);
        }
    }

    public void Remove(EnvironmentInstance instance)
    {
        if (m_environmentInstances.Contains(instance)) m_environmentInstances.Remove(instance);
    }               

    public void RandomiseTarget()
    {
        // var sortedEnvironmentInstance = m_environmentInstances.OrderBy( x => x.Priority ).ToList();
        foreach (var eo in m_environmentInstances)
        {
            eo.RandomiseTargetWorldPosition(transform, m_randomZone);
            eo.RandomiseTargetRotation(transform, m_randomRotationRange.x, m_randomRotationRange.y);
            eo.RandomiseTargetScale(transform, m_randomScaleRange.x, m_randomScaleRange.y);
        }
    }

    public void RandomiseInitial()
    {
        // var sortedEnvironmentInstance = m_environmentInstances.OrderBy( x => x.Priority ).ToList();
        foreach(var ei in m_environmentInstances)
        {

            ei.RandomiseInitialWorldPosition(transform, m_randomZone);
            ei.RandomiseInitialRotation(transform, m_randomRotationRange.x, m_randomRotationRange.y);
            ei.RandomiseInitialScale(transform, m_randomScaleRange.x, m_randomScaleRange.y);
            ei.Initialize(this);
        }

    }    

    public void SortEnvironmentInstances()
    {                
        m_environmentInstances = m_environmentInstances.OrderBy( x => x.Priority ).ToList();                        
    }    

     public void SetState (int state)
    {
        if (m_currentState == EControlState.NONE || m_currentState == EControlState.STOP)
        {
            CurrentState = (EControlState) state;
        }
    }    

    #if UNITY_EDITOR
    [HeaderAttribute("Debug Options")]
    public bool IsDrawSelfRotationDisc = false;
    public bool IsDrawTarget = false;
    public bool IsDrawTargetLine = false;
    public bool IsDrawParentLine = false;
    public bool IsDrawTargetParentLine = false;
    public bool IsDrawTargetRotationDisc = false;

    void OnDrawGizmos()
    {
        Handles.color = Color.yellow;
        Handles.DrawWireCube(transform.position, m_randomZone);

        foreach (var eo in m_environmentInstances)
        {
            // we need to transform to target local space, apply rotation and then
            // var initialMatrix = Gizmos.matrix;
            // var debugNScaleX = Mathf.Lerp(3.0f, 4.0f, Mathf.InverseLerp(m_randomScaleRange.x, m_randomScaleRange.y, eo.TargetWorldScale.x));
            // var debugNScaleY = Mathf.Lerp(3.0f, 4.0f, Mathf.InverseLerp(m_randomScaleRange.x, m_randomScaleRange.y, eo.TargetWorldScale.y));
            // var debugNScaleZ = Mathf.Lerp(3.0f, 4.0f, Mathf.InverseLerp(m_randomScaleRange.x, m_randomScaleRange.y, eo.TargetWorldScale.z));

            // Gizmos.color = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.5f);            
            // var amat = Matrix4x4.TRS(eo.TargetWorldPosition, Quaternion.identity, new Vector3(debugNScaleX, debugNScaleY, debugNScaleZ));
            // Gizmos.matrix = transform.localToWorldMatrix * amat;                         
            // Gizmos.DrawWireCube(Vector3.zero, UnityEngine.Vector3.one * 0.2f );
            // Gizmos.matrix = initialMatrix;

            // Gizmos.color = Color.green;
            // var tmat = Matrix4x4.TRS(eo.TargetWorldPosition, eo.TargetWorldRotation, new Vector3(debugNScaleX, debugNScaleY, debugNScaleZ));
            // Gizmos.matrix = transform.localToWorldMatrix * tmat;                         
            // Gizmos.DrawWireCube(Vector3.zero, UnityEngine.Vector3.one * 0.15f );

            // draw rotation disc and rotation axis
            if (IsDrawSelfRotationDisc)
            {
                Handles.color = Color.magenta;
                var rotationAxis = (eo.transform.parent.GetComponent<EnvironmentInstance>() != null) ? eo.transform.parent.rotation * eo.TargetRotationAxis : eo.TargetRotationAxis;
                Handles.DrawWireDisc(eo.transform.position, rotationAxis, 1.0f);
                Gizmos.color = Color.magenta;            
                Gizmos.DrawLine(eo.transform.position, eo.transform.position + rotationAxis.normalized);
            }

            if (IsDrawTarget)
            {
                Gizmos.color = (eo.transform.parent.GetComponent<EnvironmentInstance>() == null) ? Color.white : new Color(1, 1, 1, 0.25f);
                Gizmos.DrawWireSphere(eo.TargetWorldPosition, 0.2f);
            }
            if (IsDrawTargetLine)
            {
                Gizmos.color = (eo.transform.parent.GetComponent<EnvironmentInstance>() == null) ? Color.white : new Color(1, 1, 1, 0.25f);
                Gizmos.DrawLine(eo.transform.position, eo.TargetWorldPosition);                                         
            }

            // Gizmos.matrix = initialMatrix;

            // draw green lines to location
            if (IsDrawParentLine)
            {
                if (eo.transform.parent.GetComponent<EnvironmentInstance>() != null) 
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(eo.transform.position, eo.transform.parent.position);            
                }            
            }

            // draw debug line towards parent obeject
            Gizmos.color = Color.blue;
            Handles.color = new Color(1, 1, 1, 0.25f);
            var parentEO = eo.transform.parent.GetComponent<EnvironmentInstance>();
            if (parentEO != null)
            {
                var parentToChildDirection = eo.TargetWorldPosition - eo.transform.parent.GetComponent<EnvironmentInstance>().TargetWorldPosition;                
                var ptcDotRotationAxis = Vector3.Dot(parentToChildDirection, parentEO.TargetRotationAxis.normalized); // project ptcDirection to target rotation axis to find magnitude
                if (IsDrawTargetParentLine)
                {
                    Gizmos.DrawLine(eo.TargetWorldPosition, eo.transform.parent.GetComponent<EnvironmentInstance>().TargetWorldPosition);
                }                
                if (IsDrawTargetRotationDisc)
                {
                    var discPos = parentEO.TargetWorldPosition + (parentEO.TargetRotationAxis.normalized * ptcDotRotationAxis);
                    Handles.DrawWireDisc(discPos, parentEO.TargetRotationAxis, (eo.TargetWorldPosition - discPos).magnitude);            
                }
            }
        }
    }
    #endif
}
