using UnityEngine;
using UnityEngine.UI;

public class LogButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private string logText;


    public void ShowLog()
    {
        DialogueHandler.Instance.ShowLog(logText);
    }


    public void UpdateSettings(string changedSetting)
    {
        if(changedSetting == "all" || changedSetting == TheSoup.Rule)
        {
            button.gameObject.SetActive(SettingsManager.GetBool(TheSoup.Rule));
        }
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;

        if(SettingsManager.Loaded)
        {
            button.gameObject.SetActive(SettingsManager.GetBool(TheSoup.Rule));
        }
        else button.gameObject.SetActive(false);
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
    }
}