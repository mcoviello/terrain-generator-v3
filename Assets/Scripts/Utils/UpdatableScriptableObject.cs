using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableScriptableObject : ScriptableObject
{
    public event System.Action OnValuesUpdated;

    public void NotifyOfUpdatedValues()
    {
        if(OnValuesUpdated != null)
        {
            OnValuesUpdated();
        }
    }
}
