using System;
using UnityEngine;

public class WallHandler : MonoBehaviour
{
    [SerializeField] private GameObject wallBody;
    [SerializeField] private MeshRenderer bodyRenderer;

    public Action<Vector3> OnScaleUpdated;


    public void SetScale(Vector3 scale)
    {
        wallBody.transform.localScale = scale;
        OnScaleUpdated?.Invoke(scale);
    }


    public void SetMaterial(MaterialPropertyBlock properties)
    {
        bodyRenderer.SetPropertyBlock(properties);
    }
}