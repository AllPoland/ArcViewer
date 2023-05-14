using System.Collections;
using UnityEngine;

public class ShaderWarmup : MonoBehaviour
{
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject shaderObjects;
    [SerializeField] private GameObject shaderUI;


    private IEnumerator WarmupShadersCoroutine()
    {
        loadingPanel.SetActive(true);
        //Wait two frames to allow UI to render
        yield return null;
        yield return null;

        //Hack to force shaders to warm up by rendering a bunch of objects with the shaders
        shaderObjects.SetActive(true);
        shaderUI.SetActive(true);

        //Wait a few more frames to make sure the objects render
        yield return null;
        yield return null;
        yield return null;

        shaderObjects.SetActive(false);
        Destroy(shaderObjects);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }


    private void Start()
    {
        StartCoroutine(WarmupShadersCoroutine());
    }
}