using UnityEngine;

public class WallHandler : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;

    private MaterialPropertyBlock materialProperties;


    public void SetProperties(MaterialPropertyBlock properties)
    {
        meshRenderer.SetPropertyBlock(properties);
        materialProperties = properties;
    }


    public void SetAlpha(float alpha)
    {
        if(materialProperties == null)
        {
            Debug.LogWarning("Tried to set alpha on a wall with no material properties!");
            return;
        }

        Color wallColor = materialProperties.GetColor("_BaseColor");
        wallColor.a = alpha;
        materialProperties.SetColor("_BaseColor", wallColor);

        meshRenderer.SetPropertyBlock(materialProperties);
    }
}