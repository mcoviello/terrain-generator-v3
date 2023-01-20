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

        if (GUILayout.Button("Button"))
        {
            data.NotifyOfUpdatedValues();
        }
    }
}
