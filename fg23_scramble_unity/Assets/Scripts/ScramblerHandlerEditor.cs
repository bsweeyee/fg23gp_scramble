
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif


[CustomEditor(typeof(ScramblerHandler))]
public class ScramblerHandlerEditor : Editor
{
    private float m_angle;
    private float m_normalizedDt;
    private float m_previousDT;

    void OnEnable()
    {

    }

#if UNITY_EDITOR
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var em = serializedObject.targetObject.GetComponent<ScramblerHandler>();
        if (em == null) return;

        EditorGUILayout.LabelField("Preview Transform");
        m_previousDT = m_normalizedDt;
        m_normalizedDt = EditorGUILayout.Slider(m_normalizedDt, 0, 1);

        if (!Application.isPlaying)
        {
            foreach(var ei in em.EnvironmentInstances)
            {
                if (m_normalizedDt >= 0)
                {
                    ei.Move(m_normalizedDt - m_previousDT, ScramblerInstance.EMoveType.TARGET);
                }
            }
        }        

        if (GUILayout.Button("Randomise initial"))
        {
            em.RandomiseInitial();
        }

        if (GUILayout.Button("Randomise target"))
        {
            em.RandomiseTarget();
        }

        SceneView.RepaintAll();
    }
#endif
}
