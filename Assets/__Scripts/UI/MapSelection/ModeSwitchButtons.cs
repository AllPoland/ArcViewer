using UnityEngine;
using UnityEngine.UI;

public class ModeSwitchButtons : MonoBehaviour
{
    [SerializeField] private Button previewModeButton;
    [SerializeField] private Button replayModeButton;


    private void SetReplayMode(bool replayMode)
    {
        SettingsManager.SetRule("replaymode", replayMode);
#if !UNITY_WEBGL || UNITY_EDITOR
        SettingsManager.SaveSettingsStatic();
#endif
    }


    private void UpdateButtons()
    {
        bool isReplayMode = SettingsManager.GetBool("replaymode");
        previewModeButton.gameObject.SetActive(isReplayMode);
        replayModeButton.gameObject.SetActive(!isReplayMode);
    }


    private void UpdateSettings(string setting)
    {
        if(setting == "all" || setting == "replaymode")
        {
            UpdateButtons();
        }   
    }


    public void SetPreviewMode()
    {
        SetReplayMode(false);
    }


    public void SetReplayMode()
    {
        SetReplayMode(true);
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;
        if(SettingsManager.Loaded)
        {
            UpdateButtons();
        }
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;
    }
}