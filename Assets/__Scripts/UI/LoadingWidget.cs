using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingWidget : MonoBehaviour
{
    [SerializeField] private float rotationSpeed;

    private bool loading;
    private Image image;


    public void UpdateLoading(bool newLoading)
    {
        image.enabled = newLoading;
        loading = newLoading;
    }


    private void Update()
    {
        if(loading)
        {
            transform.Rotate(Vector3.forward * (rotationSpeed * Time.deltaTime));
        }
    }


    private void Start()
    {
        image = GetComponent<Image>();
    }


    private void OnEnable()
    {
        BeatmapLoader.OnLoadingChanged += UpdateLoading;
    }


    private void OnDisable()
    {
        BeatmapLoader.OnLoadingChanged -= UpdateLoading;
    }
}