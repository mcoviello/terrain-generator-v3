using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct NoiseLayer
{
    //Defines all of the noise types that the Noise Compute Shader is capable of generating.
    public enum NoiseTypes
    {
        Simplex,
        Ridged,
        Voronoi,
        Value
    }

    public string LayerName;
    public NoiseTypes NoiseType;
    public int Octaves;
    public float Frequency;
    public float Amplitude;
    public float Lacunarity;
    public float Gain;
    public bool DoimainWarp;
}
