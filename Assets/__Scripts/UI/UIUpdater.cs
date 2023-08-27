using UnityEngine;
using TMPro;

public class UIUpdater : MonoBehaviour
{
    [SerializeField] private GameObject selectionScreen;
    [SerializeField] private GameObject previewScreen;
    [SerializeField] private GameObject infoScreen;

    [Space]
    [SerializeField] private GameObject background;
    [SerializeField] private TMP_InputField directoryField;

    [Space]
    [SerializeField] private GameObject replayMapSelect;
    [SerializeField] private GameObject replayModeToggle;


    private void UpdateState(UIState newState)
    {
        if(newState == UIState.MapSelection)
        {
            directoryField.text = "";

            SongManager.Instance.DestroyClip();
            if(CoverImageHandler.Instance != null)
            {
                CoverImageHandler.Instance.ClearImage();
            }

            Resources.UnloadUnusedAssets();
        }

        selectionScreen.SetActive(newState == UIState.MapSelection && !MapLoader.Loading);
        previewScreen.SetActive(newState == UIState.Previewer);
        background.SetActive(newState == UIState.MapSelection);

        replayMapSelect.SetActive(false);
        replayModeToggle.SetActive(true);

        DialogueHandler.ClearExtraPopups();
    }


    private void UpdateLoading(bool newLoading)
    {
        selectionScreen.SetActive(UIStateManager.CurrentState == UIState.MapSelection && !MapLoader.Loading);
    }


    private void EnableReplayMapPrompt()
    {
        replayMapSelect.SetActive(true);
        replayModeToggle.SetActive(false);
    }


    private void Start()
    {
        UIStateManager.OnUIStateChanged += UpdateState;
        MapLoader.OnLoadingChanged += UpdateLoading;
        MapLoader.OnReplayMapPrompt += EnableReplayMapPrompt;

        UIStateManager.CurrentState = UIState.MapSelection;
    }


    private void OnDestroy()
    {
        UIStateManager.OnUIStateChanged -= UpdateState;
        MapLoader.OnLoadingChanged -= UpdateLoading;
        MapLoader.OnReplayMapPrompt -= EnableReplayMapPrompt;
    }
}