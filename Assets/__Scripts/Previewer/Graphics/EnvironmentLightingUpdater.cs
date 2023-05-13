using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class EnvironmentLightingUpdater : MonoBehaviour
{
    [SerializeField] private ReflectionProbe probe;
    [SerializeField] private float defaultProbeIntensity;
    [SerializeField, Range(0f, 1f)] private float ambientLightBrightness;

    private static readonly string[] lightingSettings = new string[]
    {
        "instantreflectionupdate",
        "reflectionquality",
        "lightreflectionbrightness"
    };

    private int renderId;
    private bool isRendering => !probe.IsFinishedRendering(renderId) && probe.timeSlicingMode != ReflectionProbeTimeSlicingMode.NoTimeSlicing;


    private void UpdateReflection()
    {
        renderId = probe.RenderProbe();
    }


    private IEnumerator UpdateStaticLightsCoroutine()
    {
        yield return new WaitUntil(() => !isRendering);
        UpdateReflection();
    }


    public void UpdateStaticLights()
    {
        if(LightManager.StaticLights && probe.timeSlicingMode != ReflectionProbeTimeSlicingMode.NoTimeSlicing)
        {
            //Wait for the current rendering to finish so lighting correctly updates
            StartCoroutine(UpdateStaticLightsCoroutine());
        }
        else
        {
            UpdateReflection();
        }
    }


    public void UpdateBeat(float beat)
    {
        if(!LightManager.StaticLights && probe.intensity > 0.001f)
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

            bool instantUpdate = SettingsManager.GetBool("instantreflectionupdate");
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
        else if(setting == "staticlights")
        {
            UpdateStaticLights();
        }
    }


    private void Start()
    {
        TimeManager.OnBeatChanged += UpdateBeat;
        ColorManager.OnColorsChanged += UpdateColors;
        SettingsManager.OnSettingsUpdated += UpdateSettings;
        LightManager.OnStaticLightsChanged += UpdateStaticLights;

        UpdateSettings("all");
    }


    private void OnDestroy()
    {
        TimeManager.OnBeatChanged -= UpdateBeat;
        ColorManager.OnColorsChanged -= UpdateColors;
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
        LightManager.OnStaticLightsChanged -= UpdateStaticLights;
    }
}