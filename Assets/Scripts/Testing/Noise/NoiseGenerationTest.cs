using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseGenerationTest : MonoBehaviour
{
    public ComputeShader test;
    public float Frequency;
    public float Amplitude;
    public float Lacunarity;
    public float Gain;
    public int Octaves;

    public Material visualiser;

    public RenderTexture renderTex;
    public void Awake()
    {
        if (renderTex == null)
        {
            renderTex = new RenderTexture(64, 64, 24);
            renderTex.enableRandomWrite = true;
            renderTex.Create();
        }
    }
    void Update()
    {
        UpdateNoise();
        visualiser.SetTexture("_MainTex",renderTex);
    }

    //private void OnRenderImage(RenderTexture source, RenderTexture destination)
    //{
    //    if (renderTex == null)
    //    {
    //        renderTex = new RenderTexture(64, 64, 24);
    //        renderTex.enableRandomWrite = true;
    //        renderTex.Create();
    //    }
    //
    //    Graphics.Blit(renderTex, destination);
    //}

    private void UpdateNoise()
    {
        test.SetTexture(0, "OutputNoise", renderTex);
        test.SetFloat("_Resolution", renderTex.width);
        test.SetFloat("_Frequency", Frequency);
        test.SetFloat("_Amplitude", Amplitude);
        test.SetFloat("_Lacunarity", Lacunarity);
        test.SetFloat("_Gain", Gain);
        test.SetInt("_Octaves", Octaves);
        test.SetFloat("_Time", Time.time);
        test.Dispatch(0, renderTex.width / 8, renderTex.height / 8, 1);
    }
}
