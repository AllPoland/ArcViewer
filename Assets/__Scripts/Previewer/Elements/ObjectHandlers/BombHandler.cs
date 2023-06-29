using UnityEngine;

public class BombHandler : MonoBehaviour
{
    public bool HasCustomProperties { get; private set; }

    [SerializeField] private MeshRenderer meshRenderer;


    public void SetMaterial(Material newMaterial)
    {
        meshRenderer.sharedMaterial = newMaterial;
    }


    public void SetProperties(MaterialPropertyBlock propertyBlock)
    {
        meshRenderer.SetPropertyBlock(propertyBlock);
        HasCustomProperties = true;
    }


    public void ClearProperties()
    {
        meshRenderer.SetPropertyBlock(new MaterialPropertyBlock());
        HasCustomProperties = false;
    }
}