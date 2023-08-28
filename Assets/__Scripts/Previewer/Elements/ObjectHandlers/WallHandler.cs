using UnityEngine;

public class WallHandler : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshRenderer edgeRenderer;

    private MaterialPropertyBlock materialProperties;
    private MaterialPropertyBlock edgeProperties;


    public void SetScale(Vector3 newScale)
    {
        transform.localScale = newScale;

        edgeProperties.SetVector("_WallScale", newScale);
        edgeRenderer.SetPropertyBlock(edgeProperties);
    }


    public void SetColor(Color newColor)
    {
        materialProperties.SetColor("_BaseColor", newColor);
        edgeProperties.SetColor("_BaseColor", newColor);

        meshRenderer.SetPropertyBlock(materialProperties);
        edgeRenderer.SetPropertyBlock(edgeProperties);
    }


    public void SetAlpha(float alpha)
    {
        Color wallColor = materialProperties.GetColor("_BaseColor");
        wallColor.a = alpha;

        materialProperties.SetColor("_BaseColor", wallColor);
        meshRenderer.SetPropertyBlock(materialProperties);
    }


    public void SetSortingOrder(int order)
    {
        meshRenderer.sortingOrder = order;
    }


    private void Awake()
    {
        materialProperties = new MaterialPropertyBlock();
        edgeProperties = new MaterialPropertyBlock();
    }
}