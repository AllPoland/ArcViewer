using System;
using UnityEngine;

public class WallHandler : MonoBehaviour
{
    [SerializeField] private GameObject wallBody;
    [SerializeField] private MeshRenderer bodyRenderer;
    [SerializeField] private float edgeIntensity;

    public Action<Vector3> OnScaleUpdated;
    public Action<MaterialPropertyBlock> OnPropertiesUpdated;


    public void SetScale(Vector3 scale)
    {
        wallBody.transform.localScale = scale;
        OnScaleUpdated?.Invoke(scale);
    }


    public void SetMaterial(MaterialPropertyBlock properties)
    {
        bodyRenderer.SetPropertyBlock(properties);

        float h;
        float s;
        float v;
        Color wallColor = properties.GetColor("_BaseColor");
        
        Color.RGBToHSV(wallColor, out h, out s, out v);
        v = edgeIntensity;
        Color edgeGlowColor = Color.HSVToRGB(h, s, v, true);

        properties.SetColor("_EmissionColor", edgeGlowColor);
        OnPropertiesUpdated?.Invoke(properties);
    }
}