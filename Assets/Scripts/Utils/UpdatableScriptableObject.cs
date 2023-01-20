using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableScriptableObject : ScriptableObject
{
    public event System.Action OnValuesUpdated;
    public bool AutoUpdate;

    protected virtual void OnValidate()
    {
        if (AutoUpdate)
        {
            NotifyOfUpdatedValues();
        }
    }

    public void NotifyOfUpdatedValues()
    {
        if(OnValuesUpdated != null)
        {
            OnValuesUpdated();
        }
    }
}
