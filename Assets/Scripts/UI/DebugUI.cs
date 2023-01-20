using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    [SerializeField] TMP_Text fps;
    [SerializeField] TMP_Text memory;
    [SerializeField] private int NoOfFramesToAverage;

    private Color fpsColor;

    private float[] fpsValues;
    private int currentFPSIndex;
    private float avgFPS;

    private void Awake()
    {
        fpsValues = new float[NoOfFramesToAverage];
        for(int i = 0; i < NoOfFramesToAverage; i++) {
            fpsValues[i] = -1f;
        }

        currentFPSIndex = 0;
    }
    void Update()
    {
        UpdateFPS();
        UpdateMemory();
    }

    void AverageFPS()
    {
        avgFPS = 0;
        for(int i = 0; i < NoOfFramesToAverage; i++)
        {
            if (fpsValues[i] == -1)
            {
                continue;
            }
            avgFPS += fpsValues[i];
        }

        avgFPS /= NoOfFramesToAverage;
        avgFPS = Mathf.Round(avgFPS);
    }
    void UpdateFPS()
    {
        currentFPSIndex++;
        currentFPSIndex = currentFPSIndex % (NoOfFramesToAverage);
        fpsValues[currentFPSIndex] = 1 / Time.unscaledDeltaTime;
        AverageFPS();

        if (avgFPS > 60)
        {
            fpsColor = Color.green;
        }
        else if (avgFPS > 30)
        {
            fpsColor = Color.yellow;
        }
        else
        {
            fpsColor = Color.red;
        }

        fps.text = "FPS: " + avgFPS;
        fps.color = fpsColor;
    }

    void UpdateMemory()
    {
        //Dont use force full collection, starts running in deep profiler mode!
        memory.text = "RAM Used: " + Mathf.RoundToInt(System.GC.GetTotalMemory(false) / 1000000f) + "MB";
    }
}
