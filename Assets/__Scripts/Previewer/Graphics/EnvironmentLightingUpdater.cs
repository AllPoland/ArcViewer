using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class EnvironmentLightingUpdater : MonoBehaviour
{
    public bool RealTimeReflections = false;

    [SerializeField] private ReflectionProbe probe;
    [SerializeField] private float defaultProbeIntensity;
    [SerializeField, Range(0f, 1f)] private float ambientLightBrightness;

    private static readonly string[] lightingSettings = new string[]
    {
        "realtimereflections",
        "instantreflectionupdate",
        "reflectionquality",
        "lightreflectionbrightness"
    };


    private void UpdateReflection()
    {
        probe.RenderProbe();
    }


    public void UpdateBeat(float beat)
    {
        if(RealTimeReflections)
        {
            UpdateReflection();
        }
    }


    public void UpdateColors(ColorPalette newColors)
    {
        Color redColor = newColors.LightColor1.SetValue(ambientLightBrightness);
        RenderSettings.ambientGroundColor = redColor;
        RenderSettings.ambientEquatorColor = redColor;
        RenderSettings.ambientSkyColor = newColors.LightColor2.SetValue(ambientLightBrightness);
        DynamicGI.UpdateEnvironment();
        UpdateReflection();
    }


    public void UpdateSettings(string setting)
    {
        if(setting == "all" || lightingSettings.Contains(setting))
        {
            probe.intensity = defaultProbeIntensity * SettingsManager.GetFloat("lightreflectionbrightness");
            RealTimeReflections = probe.intensity > 0.001f && SettingsManager.GetBool("realtimereflections");

            bool instantUpdate = RealTimeReflections && SettingsManager.GetBool("instantreflectionupdate");
            probe.timeSlicingMode = instantUpdate ? ReflectionProbeTimeSlicingMode.NoTimeSlicing : ReflectionProbeTimeSlicingMode.AllFacesAtOnce;

            switch(SettingsManager.GetInt("reflectionquality"))
            {
                default:
                case 0:
                    probe.resolution = 64;
                    break;
                case 1:
                    probe.resolution = 128;
                    break;
                case 2:
                    probe.resolution = 256;
                    break;
                case 3:
                    probe.resolution = 512;
                    break;
                case 4:
                    probe.resolution = 1024;
                    break;
            }
            UpdateReflection();
        }
    }


    private void Start()
    {
        TimeManager.OnBeatChanged += UpdateBeat;
        ColorManager.OnColorsChanged += UpdateColors;
        SettingsManager.OnSettingsUpdated += UpdateSettings;

        UpdateSettings("all");
    }


    private void OnDestroy()
    {
        TimeManager.OnBeatChanged -= UpdateBeat;
        ColorManager.OnColorsChanged -= UpdateColors;
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
    }
}