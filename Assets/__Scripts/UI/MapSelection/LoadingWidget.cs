using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingWidget : MonoBehaviour
{
    [SerializeField] private GameObject loadingSpin;
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private TextMeshProUGUI downloadSizeText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private float rotationSpeed;

    private bool loading;


    public void UpdateLoading(bool newLoading)
    {   
        if(!newLoading)
        {
            HideElements();
        }
        else
        {
            loadingSpin.SetActive(true);
        }

        loading = newLoading;
    }


    private void HideElements()
    {
        loadingSpin.SetActive(false);
        loadingBar.gameObject.SetActive(false);
        loadingText.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        downloadSizeText.gameObject.SetActive(false);
    }


    private void Update()
    {
        if(loading)
        {
            loadingSpin.transform.Rotate(Vector3.forward * (rotationSpeed * Time.deltaTime));

            if(MapLoader.Progress > 0)
            {
                loadingBar.gameObject.SetActive(true);
                loadingBar.value = MapLoader.Progress;
            }
            else loadingBar.gameObject.SetActive(false);

            if(MapLoader.LoadingMessage != "")
            {
                loadingText.gameObject.SetActive(true);
                loadingText.text = MapLoader.LoadingMessage;
            }
            else loadingText.gameObject.SetActive(false);

            if(WebLoader.Client != null && WebLoader.Client.IsBusy)
            {
                cancelButton.gameObject.SetActive(true);

                float downloadSize = (float)WebLoader.DownloadSize / 1000000;
                float downloadProgress = (float)WebLoader.Progress / 100;
                float downloaded = downloadSize * downloadProgress;

                downloadSizeText.text = $"{Math.Round(downloaded, 1)}MB / {Math.Round(downloadSize, 1)} MB";
                downloadSizeText.gameObject.SetActive(true);
            }
            else
            {
                cancelButton.gameObject.SetActive(false);
                downloadSizeText.gameObject.SetActive(false);
            }
        }
    }


    private void OnEnable()
    {
        MapLoader.OnLoadingChanged += UpdateLoading;

        UpdateLoading(MapLoader.Loading);
    }


    private void OnDisable()
    {
        MapLoader.OnLoadingChanged -= UpdateLoading;
    }
}