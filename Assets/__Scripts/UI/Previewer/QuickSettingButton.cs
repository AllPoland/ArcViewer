using UnityEngine;
using UnityEngine.UI;

public class QuickSettingButton : MonoBehaviour
{
    [SerializeField] private string setting;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Tooltip tooltip;

    [Space]
    [SerializeField] private Color settingOffColor;
    [SerializeField] private Color settingOnColor;

    [Space]
    [SerializeField] private string settingOffTooltip;
    [SerializeField] private string settingOnTooltip;


    public void UpdateSettings(string changedSetting)
    {
        if(changedSetting == "all" || changedSetting == setting)
        {
            UpdateButton();
        }
    }


    private void UpdateButton()
    {
        bool settingOn = SettingsManager.GetBool(setting);

        buttonImage.color = settingOn ? settingOnColor : settingOffColor;
        tooltip.Text = settingOn ? settingOnTooltip : settingOffTooltip;
    }


    public void ToggleSetting(bool updateTooltip)
    {
        bool settingOn = SettingsManager.GetBool(setting);

        SettingsManager.SetRule(setting, !settingOn);
#if !UNITY_WEBGL || UNITY_EDITOR
        SettingsManager.SaveSettingsStatic();
#endif

        if(updateTooltip)
        {
            tooltip.ForceUpdate();
        }
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;

        UpdateButton();
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
    }
}