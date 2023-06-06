using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class EnvironmentLightingUpdater : MonoBehaviour
{
    [SerializeField] private ReflectionProbe probe;
    [SerializeField] private float defaultProbeIntensity;
    [SerializeField, Range(0f, 1f)] private float ambientLightBrightness;
    [SerializeField, Range(0f, 1f)] private float noReflectionAmbientBrightness;
    [SerializeField, Range(0f, 1f)] private float ambientLightSaturation;

    private static readonly string[] lightingSettings = new string[]
    {
        "dynamicreflections",
        "instantreflectionupdate",
        "reflectionquality",
        "lightreflectionbrightness",
        "ambientlightbrightness"
    };

    private bool dynamicReflections;
    private int renderId;
    private bool isRendering => dynamicReflections && !probe.IsFinishedRendering(renderId) && probe.timeSlicingMode != ReflectionProbeTimeSlicingMode.NoTimeSlicing;
    private float ambientBrightness => (dynamicReflections ? ambientLightBrightness : noReflectionAmbientBrightness) * SettingsManager.GetFloat("ambientlightbrightness");


    private void UpdateReflection()
    {
        if(dynamicReflections)
        {
            renderId = probe.RenderProbe();
        }
    }


    private IEnumerator ForceUpdateReflectionsCoroutine()
    {
        yield return new WaitUntil(() => !isRendering);
        UpdateReflection();
    }


    public void UpdateStaticLights()
    {
        if(dynamicReflections)
        {
            if(LightManager.StaticLights && probe.timeSlicingMode != ReflectionProbeTimeSlicingMode.NoTimeSlicing)
            {
                //Wait for the current rendering to finish so lighting correctly updates
                StartCoroutine(ForceUpdateReflectionsCoroutine());
            }
            else
            {
                UpdateReflection();
            }
        }
        else
        {
            UpdateColors(ColorManager.CurrentColors);
        }
    }


    public void UpdatePlaying(bool playing)
    {
        if(!playing && !LightManager.StaticLights && probe.timeSlicingMode != ReflectionProbeTimeSlicingMode.NoTimeSlicing)
        {
            StartCoroutine(ForceUpdateReflectionsCoroutine());
        }
    }


    public void UpdateBeat(float beat)
    {
        if(!LightManager.StaticLights && probe.intensity > 0.001f)
        {
            UpdateReflection();
        }
    }


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
        UpdateReflection();
    }


    public void UpdateSettings(string setting)
    {
        if(setting == "all" || lightingSettings.Contains(setting))
        {
            dynamicReflections = SettingsManager.GetBool("dynamicreflections");
            if(dynamicReflections)
            {
                probe.intensity = defaultProbeIntensity * SettingsManager.GetFloat("lightreflectionbrightness");

                bool instantUpdate = SettingsManager.GetBool("instantreflectionupdate");
                probe.timeSlicingMode = instantUpdate ? ReflectionProbeTimeSlicingMode.NoTimeSlicing : ReflectionProbeTimeSlicingMode.AllFacesAtOnce;

                switch(SettingsManager.GetInt("reflectionquality"))
                {
                    default:
                    case 0:
                        probe.resolution = 32;
                        break;
                    case 1:
                        probe.resolution = 64;
                        break;
                    case 2:
                        probe.resolution = 128;
                        break;
                    case 3:
                        probe.resolution = 256;
                        break;
                    case 4:
                        probe.resolution = 512;
                        break;
                }
            }
            else
            {
                probe.intensity = 0f;
            }
            UpdateColors(ColorManager.CurrentColors);
        }
        else if(setting == "staticlights" || setting == "lightglowbrightness")
        {
            UpdateStaticLights();
        }
    }


    private void Start()
    {
        TimeManager.OnBeatChanged += UpdateBeat;
        TimeManager.OnPlayingChanged += UpdatePlaying;
        ColorManager.OnColorsChanged += UpdateColors;
        SettingsManager.OnSettingsUpdated += UpdateSettings;
        LightManager.OnStaticLightsChanged += UpdateStaticLights;

        UpdateSettings("all");
    }


    private void OnDestroy()
    {
        TimeManager.OnBeatChanged -= UpdateBeat;
        TimeManager.OnPlayingChanged -= UpdatePlaying;
        ColorManager.OnColorsChanged -= UpdateColors;
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
        LightManager.OnStaticLightsChanged -= UpdateStaticLights;
    }
}