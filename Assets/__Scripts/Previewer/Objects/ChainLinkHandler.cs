using UnityEngine;

public class ChainLinkHandler : MonoBehaviour
{
    public AudioSource audioSource;
    public bool Visible;

    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshRenderer dotMeshRenderer;
    [SerializeField] private GameObject dot;


    public void SetMaterial(Material newMaterial)
    {
        if(newMaterial == meshRenderer.material) return;

        meshRenderer.material = newMaterial;
    }


    public void SetDotMaterial(Material newMaterial)
    {
        if(newMaterial == dotMeshRenderer.material) return;

        dotMeshRenderer.material = newMaterial;
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