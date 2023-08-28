using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GraphicSettingsUpdater : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Volume bloomVolume;
    [SerializeField] private UniversalRenderPipelineAsset urpAsset;
    [SerializeField] private RenderTexture orthoCameraTexture;

    [Space]
    [SerializeField] private float defaultBloomStrength;

    //The number of passes used with no downsampling
    //Determines how far bloomfog blurs
    private const int baseBloomfogPasses = 20;

    //Multiplier to decrease passes when increasing downsampling
    //Used to keep the blur consistent since lower resolution needs fewer passes
    //to blur the same amount
    private const float passesMult = 0.5f * 1.2f;

    private Bloom bloom;


    private void SetOrthoCameraMSAA(int msaa)
    {
#if !UNITY_WEBGL && !UNITY_EDITOR
        orthoCameraTexture.Release();
        orthoCameraTexture.antiAliasing = msaa;
        orthoCameraTexture.Create();
#endif
    }


    public void UpdateGraphicsSettings(string setting)
    {
        bool allSettings = setting == "all";

        if(allSettings || setting == "vsync" || setting == "framecap")
        {
            bool vsync = SettingsManager.GetBool("vsync");
            QualitySettings.vSyncCount = vsync ? 1 : 0;
            if(vsync)
            {
                Application.targetFrameRate = -1;
            }
            else
            {
                int framecap = SettingsManager.GetInt("framecap");

                //Value of -1 uncaps the framerate
                if(framecap <= 0 || framecap > 200) framecap = -1;

                Application.targetFrameRate = framecap;
            }
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        if(allSettings || setting == "antialiasing")
        {
            int antiAliasing = SettingsManager.GetInt("antialiasing");
            Camera.main.allowMSAA = antiAliasing > 0;

            switch(antiAliasing)
            {
                default:
                case 0:
                    urpAsset.msaaSampleCount = 1;
                    SetOrthoCameraMSAA(1);
                    break;
                case 1:
                    urpAsset.msaaSampleCount = 2;
                    SetOrthoCameraMSAA(2);
                    break;
                case 2:
                    urpAsset.msaaSampleCount = 4;
                    SetOrthoCameraMSAA(4);
                    break;
                case 3:
                    urpAsset.msaaSampleCount = 8;
                    SetOrthoCameraMSAA(8);
                    break;
            }
        }
#else
        if(allSettings)
        {
            Camera.main.allowMSAA = false;
        }
#endif

        if(allSettings || setting == "bloom")
        {
            bloom.intensity.value = defaultBloomStrength * SettingsManager.GetFloat("bloom");
            bloom.active = bloom.intensity.value >= 0.001f;

            targetCamera.UpdateVolumeStack();
        }

        if(allSettings || setting == "renderscale")
        {
            urpAsset.renderScale = Mathf.Clamp(SettingsManager.GetFloat("renderscale"), 0.5f, 2f);
        }

        if(allSettings || setting == "upscaling")
        {
            bool useUpscaling = SettingsManager.GetBool("upscaling");
            urpAsset.upscalingFilter = useUpscaling ? UpscalingFilterSelection.FSR : UpscalingFilterSelection.Auto;
        }

        if(allSettings || setting == "bloomfogquality" || setting == "lightglowbrightness")
        {
            float bloomfogBrightness = SettingsManager.GetFloat("lightglowbrightness");
            if(bloomfogBrightness < 0.001f)
            {
                Bloomfog.Enabled = false;
            }
            else
            {
                Bloomfog.Enabled = true;
                int bloomfogQuality = SettingsManager.GetInt("bloomfogquality");

                int downsample;
                switch(bloomfogQuality)
                {
                    default:
                    case 0:
                        downsample = 16;
                        break;
                    case 1:
                        downsample = 8;
                        break;
                    case 2:
                        downsample = 4;
                        break;
                    case 3:
                        downsample = 2;
                        break;
                    case 4:
                        downsample = 1;
                        break;
                }

                int passes = baseBloomfogPasses;
                for(int i = 1; i < downsample; i *= 2)
                {
                    passes = Mathf.CeilToInt((float)passes * passesMult);
                }

                Bloomfog.Downsample = downsample;
                Bloomfog.BlurPasses = passes;
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