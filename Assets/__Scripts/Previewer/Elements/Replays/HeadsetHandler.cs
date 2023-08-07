using UnityEngine;

public class HeadsetHandler : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;

    private MaterialPropertyBlock headsetProperties;


    public void SetAlpha(float alpha)
    {
        headsetProperties.SetFloat("_Alpha", alpha);
        meshRenderer.SetPropertyBlock(headsetProperties);
    }


    private void Awake()
    {
        headsetProperties = new MaterialPropertyBlock();
    }
}