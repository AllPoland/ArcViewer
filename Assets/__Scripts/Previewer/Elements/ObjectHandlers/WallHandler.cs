using UnityEngine;

public class WallHandler : MonoBehaviour
{
    private static float centerScaleOffset = 0.001f;

    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshRenderer edgeRenderer;

    private MaterialPropertyBlock materialProperties;
    private MaterialPropertyBlock edgeProperties;


    public void SetScale(Vector3 newScale)
    {
        //Make the wall body slightly smaller than edges
        //To avoid Z fighting with touching wall edges
        Vector3 centerScale = new Vector3(newScale.x - centerScaleOffset, newScale.y - centerScaleOffset, newScale.z - centerScaleOffset);
        transform.localScale = centerScale;

        edgeRenderer.transform.localScale = new Vector3(newScale.x / centerScale.x, newScale.y / centerScale.y, newScale.z / centerScale.z);

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