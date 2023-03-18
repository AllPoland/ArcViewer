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

            if(WebLoader.uwr != null && !WebLoader.uwr.isDone)
            {
                cancelButton.gameObject.SetActive(true);

                if(WebLoader.DownloadSize > 0)
                {
                    float downloadSize = (float)WebLoader.DownloadSize / 1000000;
                    float downloaded = downloadSize * MapLoader.Progress;

                    downloadSizeText.text = $"{Math.Round(downloaded, 1)}MB / {Math.Round(downloadSize, 1)}MB";
                    downloadSizeText.gameObject.SetActive(true);
                }
                else downloadSizeText.gameObject.SetActive(false);
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