using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GraphicSettingsUpdater : MonoBehaviour
{
    [SerializeField] private Volume bloomVolume;
    [SerializeField] private UniversalRenderPipelineAsset urpAsset;
    [SerializeField] private RenderTexture orthoCameraTexture;

    [Space]
    [SerializeField] private float defaultBloomStrength;

    private Bloom bloom;


    private void SetOrthoCameraMSAA(int msaa)
    {
#if !UNITY_WEBGL && !UNITY_EDITOR
        orthoCameraTexture.Release();
        orthoCameraTexture.antiAliasing = msaa;
        orthoCameraTexture.Create();
#endif
    }


    private int GetMSAA(int antiAliasing)
    {
        switch(antiAliasing)
        {
            default:
            case 0:
                return 1;
            case 1:
                return 2;
            case 2:
                return 4;
            case 3:
                return 8;
        }
    }


    public void UpdateGraphicsSettings(string setting)
    {
        bool allSettings = setting == "all";

        if(allSettings || setting == "vsync" || setting == "framecap")
        {
            bool vsync = SettingsManager.GetBool("vsync", false);
            QualitySettings.vSyncCount = vsync ? 1 : 0;
            if(vsync)
            {
                Application.targetFrameRate = -1;
            }
            else
            {
                int framecap = SettingsManager.GetInt("framecap", false);

                //Value of -1 uncaps the framerate
                if(framecap <= 0 || framecap > 200) framecap = -1;

                Application.targetFrameRate = framecap;
            }
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        if(allSettings || setting == "antialiasing")
        {
            int antiAliasing = SettingsManager.GetInt("antialiasing", false);
            Camera.main.allowMSAA = antiAliasing > 0;

            int msaa = GetMSAA(antiAliasing);
            urpAsset.msaaSampleCount = msaa;
            SetOrthoCameraMSAA(msaa);
        }
#else
        if(allSettings)
        {
            Camera.main.allowMSAA = false;
        }
#endif

        if(allSettings || setting == "bloom")
        {
            bloom.intensity.value = defaultBloomStrength * Mathf.Clamp(SettingsManager.GetFloat("bloom"), 0f, 2f);
            bloom.active = bloom.intensity.value >= 0.001f;
        }

        if(allSettings || setting == "renderscale")
        {
            urpAsset.renderScale = Mathf.Clamp(SettingsManager.GetFloat("renderscale", false), 0.5f, 2f);
        }

        if(allSettings || setting == "upscaling")
        {
            bool useUpscaling = SettingsManager.GetBool("upscaling", false);
            urpAsset.upscalingFilter = useUpscaling ? UpscalingFilterSelection.FSR : UpscalingFilterSelection.Auto;
        }

        if(allSettings || setting == "bloomfogquality" || setting == "lightglowbrightness")
        {
            float bloomfogBrightness = Mathf.Clamp(SettingsManager.GetFloat("lightglowbrightness"), 0f, 2f);
            if(bloomfogBrightness < 0.001f)
            {
                Bloomfog.Enabled = false;
            }
            else
            {
                Bloomfog.Enabled = true;
                Bloomfog.Quality = SettingsManager.GetInt("bloomfogquality", false);
            }
        }
    }


    private void Start()
    {
        bool foundBloom = bloomVolume.profile.TryGet<Bloom>(out bloom);
        if(foundBloom)
        {
            defaultBloomStrength = bloom.intensity.value;
        }
        else
        {
            Debug.LogWarning("Unable to find bloom post processing effect!");
        }

        SettingsManager.OnSettingsUpdated += UpdateGraphicsSettings;
        if(SettingsManager.Loaded)
        {
            UpdateGraphicsSettings("all");
        }
    }
}