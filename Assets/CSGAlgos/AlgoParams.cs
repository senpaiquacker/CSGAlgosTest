using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class AlgoParams : MonoBehaviour
{
    public static float MinDist
    {
        get => Camera.main.GetComponent<AlgoParams>().UnconsiderableDistance;
    }
    
    [SerializeField, Header("Unconsidered Distance value")]
    private float mindist;
    public float UnconsiderableDistance
    {
        get => mindist;
    }
}
