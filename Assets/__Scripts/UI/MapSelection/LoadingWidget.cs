using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingWidget : MonoBehaviour
{
    [SerializeField] private GameObject loadingSpin;
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private float rotationSpeed;

    private bool loading;


    public void UpdateLoading(bool newLoading)
    {
        loadingSpin.SetActive(newLoading);
        loadingBar.gameObject.SetActive(newLoading);
        
        if(!newLoading)
        {
            loadingText.gameObject.SetActive(false);
        }

        loading = newLoading;
    }


    private void Update()
    {
        if(loading)
        {
            loadingSpin.transform.Rotate(Vector3.forward * (rotationSpeed * Time.deltaTime));

            if(BeatmapLoader.Progress > 0)
            {
                loadingBar.gameObject.SetActive(true);
                loadingBar.value = BeatmapLoader.Progress;
            }
            else loadingBar.gameObject.SetActive(false);

            if(BeatmapLoader.LoadingMessage != "")
            {
                loadingText.gameObject.SetActive(true);
                loadingText.text = BeatmapLoader.LoadingMessage;
            }
            else loadingText.gameObject.SetActive(false);
        }
    }


    private void OnEnable()
    {
        BeatmapLoader.OnLoadingChanged += UpdateLoading;

        UpdateLoading(BeatmapLoader.Loading);
    }


    private void OnDisable()
    {
        BeatmapLoader.OnLoadingChanged -= UpdateLoading;
    }
}