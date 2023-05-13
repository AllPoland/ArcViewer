using UnityEngine;

public class StaticLightsWarning : MonoBehaviour
{
    [SerializeField] private GameObject background;
    [SerializeField] private GameObject panel;


    public void UpdateSettings(string setting)
    {
        if(!SettingsManager.GetBool("staticlightswarningacknowledged"))
        {
            ShowPanel();
        }
        else gameObject.SetActive(false);
    }


    public void ShowPanel()
    {
        panel.SetActive(true);
        background.SetActive(true);
    }


    public void ClosePanel()
    {
        bool staticLights = SettingsManager.GetBool("staticlights");
        string staticString = staticLights ? "enabled" : "disabled";
        DialogueHandler.ShowDialogueBox(DialogueBoxType.Ok, $"Static lights has been {staticString}.\nYou can change this at any time in the \"visuals\" tab in the settings.");

        SettingsManager.SetRule("staticlightswarningacknowledged", true, false);
#if !UNITY_WEBGL || UNITY_EDITOR
        SettingsManager.SaveSettingsStatic();
#endif

        gameObject.SetActive(false);
    }


    private void Awake()
    {
        //This is probably bad practice, but I want to make sure the warning is first in line
        SettingsManager.OnSettingsUpdated += UpdateSettings;
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
    }
}