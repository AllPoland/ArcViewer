using UnityEngine;

public class OcclusionMarker : MonoBehaviour
{
    [SerializeField] public float intensity = 1f;
    [SerializeField] public float brightnessCap = 0f;
    [SerializeField] public float falloff = 10f;
    [SerializeField] public float falloffSteepness = 1f;
}