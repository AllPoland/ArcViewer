using UnityEngine;
using UnityEngine.UI;

public class StaticLightsButton : MonoBehaviour
{
    [SerializeField] private string staticLightsSetting;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Tooltip tooltip;

    [Space]
    [SerializeField] private Color lightsOnColor;
    [SerializeField] private Color lightsOffColor;

    [Space]
    [SerializeField] private string lightsOnTooltip;
    [SerializeField] private string lightsOffTooltip;


    public void UpdateSettings(string changedSetting)
    {
        if(changedSetting == "all" || changedSetting == staticLightsSetting)
        {
            UpdateButton();
        }
    }


    private void UpdateButton()
    {
        bool staticLights = SettingsManager.GetBool(staticLightsSetting);

        buttonImage.color = staticLights ? lightsOffColor : lightsOnColor;

        tooltip.Text = staticLights ? lightsOffTooltip : lightsOnTooltip;
    }


    public void ToggleStaticLights()
    {
        bool staticLights = SettingsManager.GetBool(staticLightsSetting);

        SettingsManager.SetRule(staticLightsSetting, !staticLights);
#if !UNITY_WEBGL || UNITY_EDITOR
        SettingsManager.SaveSettingsStatic();
#endif

        tooltip.ForceUpdate();
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
    }
}