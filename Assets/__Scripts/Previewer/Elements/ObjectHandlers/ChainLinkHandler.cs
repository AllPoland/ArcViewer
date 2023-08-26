using UnityEngine;

public class ChainLinkHandler : MonoBehaviour
{
    public AudioSource audioSource;
    public bool Visible { get; private set; }

    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshRenderer dotMeshRenderer;
    [SerializeField] private MeshRenderer outlineRenderer;

    private bool outline;
    private Color outlineColor;

    private MaterialPropertyBlock outlineProperties;


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


    public void SetOutline(bool useOutline)
    {
        outline = useOutline;
        outlineRenderer.gameObject.SetActive(useOutline);
    }


    public void SetOutline(bool useOutline, Color color)
    {
        outline = useOutline;
        if(outline)
        {
            outlineColor = color;

            if(outlineProperties == null)
            {
                outlineProperties = new MaterialPropertyBlock();
            }
            outlineProperties.SetColor("_BaseColor", outlineColor);

            outlineRenderer.gameObject.SetActive(true);
            outlineRenderer.SetPropertyBlock(outlineProperties);
        }
        else
        {
            outlineRenderer.gameObject.SetActive(false);
        }
    }


    public void DisableVisual()
    {
        if(!Visible) return;

        dotMeshRenderer.gameObject.SetActive(false);
        outlineRenderer.gameObject.SetActive(false);
        meshRenderer.enabled = false;
        Visible = false;
    }


    public void EnableVisual()
    {
        if(Visible) return;

        SetOutline(outline);
        dotMeshRenderer.gameObject.SetActive(true);
        meshRenderer.enabled = true;
        Visible = true;
    }
}