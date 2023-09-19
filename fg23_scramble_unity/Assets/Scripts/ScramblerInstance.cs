using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEngine.Animations;
using UnityEngine.UIElements;

[Serializable]
[ExecuteInEditMode]
public class ScramblerInstance : MonoBehaviour
{
    public enum EMoveType
    {
        NONE,
        TARGET,
        INITIAL
    }


    [Header("Settings")]
    [SerializeField] private string m_id;

    // assume constant speed for now
    [SerializeField] private float m_initialTranslationSpeed = 1.0f;
    [SerializeField] private float m_initialRotationSpeed = 1.0f;
    [SerializeField] private float m_initialScaleSpeed = 1.0f;

    [SerializeField] private Vector3 m_initialWorldPosition;
    [SerializeField] private Quaternion m_initialLocalRotation;
    [SerializeField] private Vector3 m_initialScale;

    [SerializeField] private Vector3 m_targetWorldPosition;
    [SerializeField] private Quaternion m_targetLocalRotation;
    [SerializeField] private Vector3 m_targetLocalScale = Vector3.one;
    [SerializeField] private Vector3 m_targetRotationAxis;

    private ScramblerHandler m_em;

    private float m_accumulatedDt;
    private int m_sortPriority;

    private Vector3 m_startWorldPosition;
    private Quaternion m_startLocalRotation;
    private Vector3 m_startScale;

    public string ID
    {
        get { return m_id; }
    }

    public Vector3 TargetWorldPosition
    {        
        get { return Matrix4x4.Rotate(transform.parent.rotation).MultiplyPoint3x4(m_targetWorldPosition); }
    }

    public Quaternion TargetWorldRotation
    {
        get { return transform.parent.rotation * m_targetLocalRotation; }
    }

    public Vector3 TargetLocalScale
    {
        get { return m_targetLocalScale; }
    }

    public Vector3 TargetRotationAxis
    {
        get { return m_targetRotationAxis; }
    }

    public Vector3 StartWorldPosition { get { return m_startWorldPosition; } }

    public Vector3 InitialWorldPosition { get { return m_initialWorldPosition; } }

    public int Priority { get { return m_sortPriority; } set { m_sortPriority = value; } }

    void OnDestroy()
    {
        // TODO: remove from em
        if (m_em != null) m_em.Remove(this);
    }

    void OnTransformChildrenChanged()
    {
        if (m_em == null) m_em = GameObject.FindAnyObjectByType<ScramblerHandler>(); // TODO: find a way to assign it without doing a find
        m_em.Populate(m_em.transform, 0, true);
        m_em.SortEnvironmentInstances();
    }

    void Update()
    {
        if (!Application.isPlaying)
        {            
            if (m_accumulatedDt <= 0)
            {                                   
                m_initialWorldPosition = transform.position;        
                m_initialLocalRotation = transform.localRotation;
                m_initialScale = transform.localScale;

                m_startWorldPosition = m_initialWorldPosition;
                m_startLocalRotation = m_initialLocalRotation;
                m_startScale = m_initialScale;
            }
        }
    }


    public void Initialize(ScramblerHandler em, bool isResetUID = false)
    {
        m_em = em;

        m_startWorldPosition = m_initialWorldPosition;
        m_startLocalRotation = m_initialLocalRotation;
        m_startScale = m_initialScale;

        if (isResetUID || string.IsNullOrEmpty(m_id)) m_id = Guid.NewGuid().ToString();
    }

    public void Initialize(ScramblerHandler em, int priority, bool isFromInstance, bool isResetUID = false)
    {
        m_em = em;        

        // we do this isFromInstance check because when initialized from OnTransformChildrenChanged Event, transform of newly spawn child is not yet properly populated with parent data
        m_initialWorldPosition = (isFromInstance) ? transform.parent.position : transform.position;        
        m_initialLocalRotation = (isFromInstance) ? transform.parent.localRotation : transform.localRotation;
        m_initialScale = (isFromInstance) ? transform.parent.localScale : transform.localScale;

        m_startWorldPosition = m_initialWorldPosition;
        m_startLocalRotation = m_initialLocalRotation;
        m_startScale = m_initialScale;

        if (priority >= 0) m_sortPriority = priority;

        RandomiseTargetWorldPosition(em.transform, em.RandomZone);
        RandomiseTargetRotation(em.transform, em.RandomRotationRange.x, em.RandomRotationRange.y);
        RandomiseTargetScale(em.transform, em.RandomScaleRange.x, em.RandomScaleRange.y);

        // TODO: check if id is unique in em
         if (isResetUID || string.IsNullOrEmpty(m_id)) m_id = Guid.NewGuid().ToString();
        // Debug.Log(ID + ": " + transform.position + ", ");
    }

    public void RandomiseTargetWorldPosition(Transform parent, Vector3 zone)
    {
        var rx = UnityEngine.Random.Range(-zone.x/2, zone.x/2);
        var ry = UnityEngine.Random.Range(-zone.y/2, zone.y/2);
        var rz = UnityEngine.Random.Range(-zone.z/2, zone.z/2);

        var targetPosition = parent.transform.position + new Vector3(rx, ry, rz);
        // m_targetWorldPosition = parent.position + parent.TransformVector(targetLocalPosition);
        m_targetWorldPosition = targetPosition;
    }

    public void RandomiseTargetRotation(Transform parent, float min, float max)
    {
        var rx = UnityEngine.Random.Range(min, max);
        var ry = UnityEngine.Random.Range(min, max);
        var rz = UnityEngine.Random.Range(min, max);

        var targetAxis = new Vector3(rx, ry, rz);        
        var targetAngle = UnityEngine.Random.Range(0.0f, 360.0f);

        m_targetLocalRotation = transform.localRotation * Quaternion.AngleAxis(targetAngle, targetAxis.normalized);
        m_targetRotationAxis = transform.localRotation * targetAxis;
    }

    public void RandomiseTargetScale(Transform parent, float min, float max)
    {
        var rs = UnityEngine.Random.Range(min, max);

        var targetScale = new Vector3(rs, rs, rs);
        m_targetLocalScale = targetScale;
    }

    public void RandomiseInitialWorldPosition(Transform parent, Vector3 zone)
    {
        var rx = UnityEngine.Random.Range(-zone.x/2, zone.x/2);
        var ry = UnityEngine.Random.Range(-zone.y/2, zone.y/2);
        var rz = UnityEngine.Random.Range(-zone.z/2, zone.z/2);

    //    transform.position = new Vector3(rx, ry, rz);
       var initialWorldPosition = parent.transform.position + new Vector3(rx, ry, rz);
       m_initialWorldPosition = initialWorldPosition;
       transform.position = m_initialWorldPosition;
       //    Debug.Log("randomise initial: " + (m_startLocalPosition - transform.localPosition).magnitude);
    }

    public void RandomiseInitialRotation(Transform parent, float min, float max)
    {
        var rx = UnityEngine.Random.Range(min, max);
        var ry = UnityEngine.Random.Range(min, max);
        var rz = UnityEngine.Random.Range(min, max);

        var targetAxis = new Vector3(rx, ry, rz);        
        var targetAngle = UnityEngine.Random.Range(0.0f, 360.0f);

        m_initialLocalRotation = Quaternion.AngleAxis(targetAngle, targetAxis.normalized);
        transform.rotation = m_initialLocalRotation;        
    }

    public void RandomiseInitialScale(Transform parent, float min, float max)
    {
        var rs = UnityEngine.Random.Range(min, max);

        var targetScale = new Vector3(rs, rs, rs);
        transform.localScale = targetScale;
        m_initialScale = targetScale;
    }

    public void SetInitialWorldPosition(Vector3 worldPosition) {
        m_initialWorldPosition = worldPosition;
        m_startWorldPosition = m_initialWorldPosition;
        transform.position = m_initialWorldPosition;
    }

    public void SetInitialRotation(Vector3 euler) {
        m_initialLocalRotation = Quaternion.Euler(euler);
        m_startLocalRotation = m_initialLocalRotation;
        transform.rotation = m_initialLocalRotation;
    }

    public void SetInitialScale(Vector3 scale) {
        m_initialScale = scale;
        m_startScale = m_initialScale;
        transform.localScale = m_initialScale;
    }

    public void Move(float dt, EMoveType moveType)
    {
        Vector3 startWorldPosition = Vector3.zero, targetWorldPosition = Vector3.zero;
        Quaternion startRotation = Quaternion.identity, targetRotation = Quaternion.identity;
        Vector3 startScale = Vector3.one, targetScale = Vector3.one;

        switch (moveType)
        {
            case EMoveType.TARGET:
                startWorldPosition = m_startWorldPosition;
                targetWorldPosition = Matrix4x4.Rotate(transform.parent.rotation).MultiplyPoint3x4(m_targetWorldPosition);

                startRotation = m_startLocalRotation;
                targetRotation = m_targetLocalRotation;

                startScale = m_startScale;
                targetScale = m_targetLocalScale;
                break;
            case EMoveType.INITIAL:
                startWorldPosition = Matrix4x4.Rotate(transform.parent.rotation).MultiplyPoint3x4(m_targetWorldPosition);
                targetWorldPosition = m_initialWorldPosition;

                startRotation = m_targetLocalRotation;
                targetRotation = m_initialLocalRotation;

                startScale = m_targetLocalScale;
                targetScale = m_initialScale;
                break;
        }

        m_accumulatedDt += dt;        

        if (m_accumulatedDt > 0)
        {            
            // transform scale
            var localScale = Vector3.Lerp(startScale, targetScale, m_accumulatedDt);
            transform.localScale = localScale;
            // transform rotation
            var localRotation = Quaternion.Lerp(startRotation, targetRotation, m_accumulatedDt);
            transform.rotation = transform.parent.rotation * localRotation;
            // transform position
            var worldPos = Vector3.Lerp(startWorldPosition, targetWorldPosition, m_accumulatedDt);                    
            transform.position = worldPos;        
        }
        else
        {
            m_accumulatedDt = 0;
        }
    }

    public void Realign(EMoveType moveType)
    {
        m_accumulatedDt = 0;

        switch (moveType)
        {
            case EMoveType.TARGET:
                m_startWorldPosition = Matrix4x4.Rotate(transform.parent.rotation).MultiplyPoint3x4(m_targetWorldPosition);
                m_startLocalRotation = m_targetLocalRotation;
                m_startScale = m_targetLocalScale;
                break;
            case EMoveType.INITIAL:
                m_startWorldPosition = m_initialWorldPosition;
                m_startLocalRotation = m_initialLocalRotation;
                m_startScale = m_initialScale;
                break;
        }

        transform.position = m_startWorldPosition;
        transform.rotation = transform.parent.rotation * m_startLocalRotation;
        transform.localScale = m_startScale;
    }
}
