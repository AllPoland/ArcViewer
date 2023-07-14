using UnityEngine;

public class BombHandler : MonoBehaviour
{
    public bool HasCustomProperties { get; private set; }
    public bool Visible => meshRenderer.enabled;

    [SerializeField] public AudioSource audioSource;
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


    public void DisableVisual()
    {
        if(!Visible) return;
        meshRenderer.enabled = false;
    }


    public void EnableVisual()
    {
        if(Visible) return;
        meshRenderer.enabled = true;
    }
}