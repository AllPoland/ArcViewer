using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GraphicSettingsUpdater : MonoBehaviour
{
    [SerializeField] private VolumeProfile mainBloomVolume;
    [SerializeField] private UniversalRenderPipelineAsset urpAsset;

    private Camera mainCamera;
    private Bloom bloom;
    private float defaultBloomStrength;


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
            mainCamera.allowMSAA = antiAliasing > 0;

            switch(antiAliasing)
            {
                case <= 0:
                    urpAsset.msaaSampleCount = 0;
                    break;
                case 1:
                    urpAsset.msaaSampleCount = 2;
                    break;
                case 2:
                    urpAsset.msaaSampleCount = 4;
                    break;
                case >= 3:
                    urpAsset.msaaSampleCount = 8;
                    break;
            }
        }
#else
        if(allSettings)
        {
            mainCamera.allowMSAA = false;
        }
#endif

#if !UNITY_EDITOR
        if(allSettings || setting == "bloom")
        {
            float mainBloomStrength = SettingsManager.GetFloat("bloom");
            bloom.active = mainBloomStrength > 0;
            bloom.intensity.value = mainBloomStrength * defaultBloomStrength;
        }
#endif
    }


    private void Start()
    {   
        mainCamera = Camera.main;

        bool foundBloom = mainBloomVolume.TryGet<Bloom>(out bloom);

        if(foundBloom)
        {
            defaultBloomStrength = bloom.intensity.value;
        }

        SettingsManager.OnSettingsUpdated += UpdateGraphicsSettings;
        UpdateGraphicsSettings("all");
    }
}