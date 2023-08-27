using System.Linq;
using UnityEngine;

public class EnvironmentLightingUpdater : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float ambientLightBrightness;
    [SerializeField, Range(0f, 1f)] private float ambientLightSaturation;

    private static readonly string[] lightingSettings = new string[]
    {
        "ambientlightbrightness"
    };

    private float ambientBrightness => ambientLightBrightness * SettingsManager.GetFloat("ambientlightbrightness");


    private void SetGradient(ColorPalette colors, float brightness)
    {
        Color redColor = colors.LightColor1.SetHSV(null, ambientLightSaturation, ambientBrightness);
        Color blueColor = colors.LightColor2.SetHSV(null, ambientLightSaturation, ambientBrightness);
        RenderSettings.ambientGroundColor = redColor;
        RenderSettings.ambientEquatorColor = Color.Lerp(redColor, blueColor, 0.5f);
        RenderSettings.ambientSkyColor = blueColor;
        DynamicGI.UpdateEnvironment();
    }


    public void UpdateColors(ColorPalette newColors)
    {
        SetGradient(newColors, ambientBrightness);
    }


    public void UpdateSettings(string setting)
    {
        if(setting == "all" || lightingSettings.Contains(setting))
        {
            UpdateColors(ColorManager.CurrentColors);
        }
    }


    private void Start()
    {
        ColorManager.OnColorsChanged += UpdateColors;
        SettingsManager.OnSettingsUpdated += UpdateSettings;

        UpdateSettings("all");
    }


    private void OnDestroy()
    {
        ColorManager.OnColorsChanged -= UpdateColors;
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
    }
}