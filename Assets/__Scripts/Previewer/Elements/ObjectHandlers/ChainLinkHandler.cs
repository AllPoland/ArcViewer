using UnityEngine;

public class ChainLinkHandler : MonoBehaviour
{
    public AudioSource audioSource;
    public bool Visible { get; private set; }

    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshRenderer dotMeshRenderer;
    [SerializeField] private GameObject dot;


    public void SetMaterial(Material newMaterial)
    {
        meshRenderer.sharedMaterial = newMaterial;
    }


    public void SetProperties(MaterialPropertyBlock properties)
    {
        meshRenderer.SetPropertyBlock(properties);
    }


    public void SetDotProperties(MaterialPropertyBlock properties)
    {
        dotMeshRenderer.SetPropertyBlock(properties);
    }


    public void DisableVisual()
    {
        if(!Visible) return;

        dot.SetActive(false);
        meshRenderer.enabled = false;
        Visible = false;
    }


    public void EnableVisual()
    {
        if(Visible) return;

        dot.SetActive(true);
        meshRenderer.enabled = true;
        Visible = true;
    }
}