using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIUpdater : MonoBehaviour
{
    [SerializeField] private GameObject selectionScreen;
    [SerializeField] private GameObject previewScreen;
    [SerializeField] private GameObject background;
    [SerializeField] private TMP_InputField directoryField;


    public void UpdateState(UIState newState)
    {
        if(newState == UIState.MapSelection)
        {
            directoryField.text = "";
        }

        selectionScreen.SetActive(newState == UIState.MapSelection && !BeatmapLoader.Loading);
        previewScreen.SetActive(newState == UIState.Previewer);
        background.SetActive(newState == UIState.MapSelection);
    }


    public void UpdateLoading(bool newLoading)
    {
        selectionScreen.SetActive(UIStateManager.CurrentState == UIState.MapSelection && !BeatmapLoader.Loading);
    }


    private void Start()
    {
        UIStateManager.OnUIStateChanged += UpdateState;
        BeatmapLoader.OnLoadingChanged += UpdateLoading;

        UIStateManager.CurrentState = UIState.MapSelection;
    }


    private void OnDisable()
    {
        UIStateManager.OnUIStateChanged -= UpdateState;
        BeatmapLoader.OnLoadingChanged -= UpdateLoading;
    }
}