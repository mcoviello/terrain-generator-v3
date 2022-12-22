using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    [SerializeField] TMP_Text fps;
    [SerializeField] TMP_Text memory;

    private Color fpsColor;

    private void Start()
    {
    }
    void Update()
    {
        UpdateFPS();
        UpdateMemory();
    }

    void UpdateFPS()
    {
        float fpsVal = Mathf.RoundToInt(1 / Time.unscaledDeltaTime);

        if (fpsVal > 60)
        {
            fpsColor = Color.green;
        }
        else if (fpsVal > 30)
        {
            fpsColor = Color.yellow;
        }
        else
        {
            fpsColor = Color.red;
        }

        fps.text = "FPS: " + fpsVal.ToString();
        fps.color = fpsColor;
    }

    void UpdateMemory()
    {
        //Dont use force full collection, starts running in deep profiler mode!
        memory.text = "RAM Used: " + Mathf.RoundToInt(System.GC.GetTotalMemory(false) / 1000000f) + "MB";
    }
}
