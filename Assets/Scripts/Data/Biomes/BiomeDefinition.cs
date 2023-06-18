using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu]
public class BiomeDefinition : UpdatableScriptableObject
{
    /* Defines the details of a biome type.
     * Includes the range of humidity and temperature where the biome can spawn,
     * and the noise used in the biome's terrain.
     */

    public float BiomeHumidityMin;
    public float BiomeHumidityMax;

    public float BiomeTemperatureMin;
    public float BiomeTemperatureMax;

    public float BiomeAltitudeMin;
    public float BiomeAltitudeMax;

    public NoiseLayer[] NoiseLayers;
}
