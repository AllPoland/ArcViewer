using System.Collections.Generic;
using UnityEngine;

public class SaberHandler : MonoBehaviour
{
    [SerializeField] private SaberTrailMeshBuilder meshBuilder;

    [Space]
    [SerializeField] private MeshRenderer bladeRenderer;
    [SerializeField] private MeshRenderer handleRenderer;
    [SerializeField] private MeshRenderer trailRenderer;

    private MaterialPropertyBlock saberProperties;
    private MaterialPropertyBlock trailProperties;


    public void SetFrames(List<ReplayFrame> frames, int startIndex) => meshBuilder.SetFrames(frames, startIndex);


    public void SetSaberProperties(Color color)
    {
        saberProperties.SetColor("_BaseColor", color);
        bladeRenderer.SetPropertyBlock(saberProperties);
        handleRenderer.SetPropertyBlock(saberProperties);
    }


    public void SetTrailProperties(Color color, Texture2D texture)
    {
        trailProperties.SetColor("_BaseColor", color);
        trailProperties.SetTexture("_TrailTexture", texture);
        trailRenderer.SetPropertyBlock(trailProperties);
    }


    private void Awake()
    {
        saberProperties = new MaterialPropertyBlock();
        trailProperties = new MaterialPropertyBlock();
    }
}