using System;
using UnityEngine;

public class WallHandler : MonoBehaviour
{
    [SerializeField] private GameObject wallBody;
    [SerializeField] private MeshRenderer bodyRenderer;

    public Action<Vector3> OnScaleUpdated;
    public Action<MaterialPropertyBlock> OnEdgePropertiesUpdated;


    public void SetScale(Vector3 scale)
    {
        wallBody.transform.localScale = scale;
        OnScaleUpdated?.Invoke(scale);
    }


    public void SetProperties(MaterialPropertyBlock properties)
    {
        bodyRenderer.SetPropertyBlock(properties);
    }


    public void SetEdgeProperties(MaterialPropertyBlock properties)
    {
        OnEdgePropertiesUpdated?.Invoke(properties);
    }
}