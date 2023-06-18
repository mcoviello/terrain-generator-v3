using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UpdatableScriptableObject),true)]
public class UpdatableScriptableObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        UpdatableScriptableObject data = (UpdatableScriptableObject) target;
        GUILayout.Space(10);
        if (GUILayout.Button("Manually Update Values"))
        {
            data.NotifyOfUpdatedValues();
        }
    }
}
