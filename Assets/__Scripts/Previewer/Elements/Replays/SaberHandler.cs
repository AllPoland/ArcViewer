using System.Collections.Generic;
using UnityEngine;

public class SaberHandler : MonoBehaviour
{
    [SerializeField] private SaberTrailMeshBuilder meshBuilder;
    [SerializeField] private GameObject saberModelContainer;

    [Space]
    [SerializeField] private MeshRenderer bladeRenderer;
    [SerializeField] private MeshRenderer handleRenderer;
    [SerializeField] private MeshRenderer trailRenderer;

    private MaterialPropertyBlock saberProperties;
    private MaterialPropertyBlock trailProperties;


    public void SetFrames(List<ReplayFrame> frames, int startIndex) => meshBuilder.SetFrames(frames, startIndex);


    public void SetTrailActive(bool active) => trailRenderer.gameObject.SetActive(active);


    public void SetSaberProperties(Color color)
    {
        saberProperties.SetColor("_BaseColor", color);
        bladeRenderer.SetPropertyBlock(saberProperties);
        handleRenderer.SetPropertyBlock(saberProperties);
    }


    public void SetTrailProperties(Color color, float brightness, Texture2D texture)
    {
        trailProperties.SetColor("_BaseColor", color);
        trailProperties.SetFloat("_Brightness", brightness);
        trailProperties.SetTexture("_TrailTexture", texture);
        trailRenderer.SetPropertyBlock(trailProperties);
    }


    public void SetWidth(float width)
    {
        Vector3 scale = saberModelContainer.transform.localScale;
        scale.x = width;
        scale.y = width;
        saberModelContainer.transform.localScale = scale;
    }


    private void Awake()
    {
        saberProperties = new MaterialPropertyBlock();
        trailProperties = new MaterialPropertyBlock();
    }
}