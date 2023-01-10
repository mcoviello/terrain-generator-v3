using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseGenerator : Singleton<NoiseGenerator>
{
    public ComputeShader test;
    public float Frequency;
    public float Amplitude;
    public float Lacunarity;
    public float Gain;
    public int Octaves;

    public void Awake()
    {
    }

    public void GenerateNoiseForChunk(Vector2Int chunkCoords, RenderTexture heightMapToWrite)
    {
        UpdateNoiseValues();
        float chunkSize = TerrainGenerationManager.Instance.ChunkSize;
        test.SetInts("_ChunkCoords", chunkCoords.x, chunkCoords.y);
        test.SetInt("_ChunkSize", TerrainGenerationManager.Instance.VerticesAlongEdge);
        test.SetTexture(0, "_ChunkHeightMap", heightMapToWrite);
        test.Dispatch(0, heightMapToWrite.width / 8, heightMapToWrite.height / 8, 1);
    }

    public void UpdateNoiseValues()
    {
        //Noise Settings
        test.SetFloat("_Frequency", Frequency);
        test.SetFloat("_Amplitude", Amplitude);
        test.SetFloat("_Lacunarity", Lacunarity);
        test.SetFloat("_Gain", Gain);
        test.SetInt("_Octaves", Octaves);
    }
}
