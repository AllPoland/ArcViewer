using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GraphicSettingsUpdater : MonoBehaviour
{
    [SerializeField] private VolumeProfile mainBloomVolume;
    [SerializeField] private VolumeProfile backgroundBloomVolume;
    [SerializeField] private UniversalRenderPipelineAsset urpAsset;

    private Camera mainCamera;
    private Bloom mainBloom;
    private float defaultBloomStrength;
    private Bloom backgroundBloom;
    private float defaultBackgroundBloomStrength;


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
            mainBloom.active = mainBloomStrength > 0;
            mainBloom.intensity.value = mainBloomStrength * defaultBloomStrength;
        }
        if(allSettings || setting == "backgroundbloom")
        {
            float backgroundBloomStrength = SettingsManager.GetFloat("backgroundbloom");
            backgroundBloom.active = backgroundBloomStrength > 0;
            backgroundBloom.intensity.value = backgroundBloomStrength * defaultBackgroundBloomStrength;
        }
#endif
    }


    private void Start()
    {   
        mainCamera = Camera.main;

        bool foundMainBloom = mainBloomVolume.TryGet<Bloom>(out mainBloom);
        bool foundBackgroundBloom = backgroundBloomVolume.TryGet<Bloom>(out backgroundBloom);

        if(foundMainBloom)
        {
            defaultBloomStrength = mainBloom.intensity.value;
        }
        if(foundBackgroundBloom)
        {
            defaultBackgroundBloomStrength = backgroundBloom.intensity.value;
        }

        SettingsManager.OnSettingsUpdated += UpdateGraphicsSettings;
        UpdateGraphicsSettings("all");
    }
}