using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util : MonoBehaviour
{
    public static float CalculateLODMultiplier(int CurrentLOD)
    {
        return 1 / (float)Mathf.Pow(2, CurrentLOD);
    }
}
