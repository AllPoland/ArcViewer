using System;
using UnityEngine;

public class OcclusionMarker : MonoBehaviour
{
    [NonSerialized] public bool Dirty = true;

    [SerializeField] public float intensity = 1f;
    [SerializeField] public float brightnessCap = 0f;
    [SerializeField] public float falloff = 10f;
    [SerializeField] public float falloffSteepness = 1f;


    private void OnEnable()
    {
        Dirty = true;
    }
}