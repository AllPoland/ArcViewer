using UnityEngine;
using TMPro;

public class UIUpdater : MonoBehaviour
{
    [SerializeField] private GameObject selectionScreen;
    [SerializeField] private GameObject previewScreen;
    [SerializeField] private GameObject infoScreen;
    [SerializeField] private GameObject background;
    [SerializeField] private TMP_InputField directoryField;


    public void UpdateState(UIState newState)
    {
        if(newState == UIState.MapSelection)
        {
            directoryField.text = "";

            AudioManager.Instance.DestroyClip();
            if(CoverImageHandler.Instance != null)
            {
                CoverImageHandler.Instance.ClearImage();
            }

            Resources.UnloadUnusedAssets();
        }

        selectionScreen.SetActive(newState == UIState.MapSelection && !MapLoader.Loading);
        previewScreen.SetActive(newState == UIState.Previewer);
        background.SetActive(newState == UIState.MapSelection);

        DialogueHandler.ClearExtraPopups();
    }


    public void UpdateLoading(bool newLoading)
    {
        selectionScreen.SetActive(UIStateManager.CurrentState == UIState.MapSelection && !MapLoader.Loading);
    }


    private void Start()
    {
        UIStateManager.OnUIStateChanged += UpdateState;
        MapLoader.OnLoadingChanged += UpdateLoading;

        UIStateManager.CurrentState = UIState.MapSelection;
    }


    private void OnDisable()
    {
        UIStateManager.OnUIStateChanged -= UpdateState;
        MapLoader.OnLoadingChanged -= UpdateLoading;
    }
}