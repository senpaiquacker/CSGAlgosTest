using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class AlgoParams : MonoBehaviour
{
    public static float MinDist
    {
        get => Camera.main.GetComponent<AlgoParams>().mindist;
    }
    public static float DebugStepTime
    {
        get => Camera.main.GetComponent<AlgoParams>().time;
    }
    [SerializeField, Header("Unconsidered Distance value")]
    private float mindist;
    [SerializeField, Header("Debug Step Timer")]
    private float time;
    public float UnconsiderableDistance
    {
        get => mindist;
    }
}
