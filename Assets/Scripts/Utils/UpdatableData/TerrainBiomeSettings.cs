using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu]
public class TerrainBiomeSettings : UpdatableScriptableObject
{
    [Header("Biome Generation Parameters")]
    public float HumidityFrequency;
    public float TemperatureFrequency;

    public BiomeDefinition[] BiomeDefinitions;

    protected void OnValidate()
    {
        HumidityFrequency = Mathf.Max(0, HumidityFrequency);
        TemperatureFrequency = Mathf.Max(0, TemperatureFrequency);
    }
}
