using TMPro;
using UnityEngine;

public class ModeSwitchButtons : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;


    public void SetReplayMode(int replayMode)
    {
        SettingsManager.SetRule("replaymode", replayMode);
    }


    private void UpdateDropdown()
    {
        dropdown.SetValueWithoutNotify(SettingsManager.GetInt("replaymode"));
    }


    private void UpdateSettings(string setting)
    {
        if(setting == "all" || setting == "replaymode")
        {
            UpdateDropdown();
        }   
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;
        if(SettingsManager.Loaded)
        {
            UpdateDropdown();
        }
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;
    }
}