using UnityEngine;

public class StaticLightsWarning : MonoBehaviour
{
    [SerializeField] private GameObject background;
    [SerializeField] private GameObject panel;


    public void CheckShowWarning()
    {
        if(!SettingsManager.GetBool("staticlightswarningacknowledged", false))
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
        bool staticLights = SettingsManager.GetBool("staticlights", false);
        string staticString = staticLights ? "enabled" : "disabled";
        DialogueHandler.ShowDialogueBox(DialogueBoxType.Ok, $"Static lights has been {staticString}.\nYou can change this at any time with the button in the bottom right corner.");

        SettingsManager.SetRule("staticlightswarningacknowledged", true, false);

        gameObject.SetActive(false);
    }


    private void Start()
    {
        CheckShowWarning();
    }
}