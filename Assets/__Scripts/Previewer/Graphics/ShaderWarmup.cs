using System.Collections;
using UnityEngine;

public class ShaderWarmup : MonoBehaviour
{
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private ShaderVariantCollection[] shaderCollections;


    private IEnumerator WarmupShadersCoroutine()
    {
        loadingPanel.SetActive(true);
        //Wait two frames to allow UI to render
        yield return null;
        yield return null;

        foreach(ShaderVariantCollection collection in shaderCollections)
        {
            collection.WarmUp();
            yield return null;
        }

        gameObject.SetActive(false);
        Destroy(gameObject);
    }


    private void Start()
    {
        StartCoroutine(WarmupShadersCoroutine());
    }
}