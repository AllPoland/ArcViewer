using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GraphicSettingsUpdater : MonoBehaviour
{
    [SerializeField] private VolumeProfile mainBloomVolume;
    [SerializeField] private VolumeProfile backgroundBloomVolume;
    [SerializeField] private UniversalRenderPipelineAsset urpAsset;
    // [SerializeField] private ScriptableRendererFeature ssaoFeature;

    private Camera mainCamera;
    private Bloom mainBloom;
    private float defaultBloomStrength;
    private Bloom backgroundBloom;
    private float defaultBackgroundBloomStrength;


    public void UpdateGraphicsSettings()
    {
        bool vsync = SettingsManager.GetBool("vsync");

        QualitySettings.vSyncCount = vsync ? 1 : 0;

        if(!vsync)
        {
            int framecap = SettingsManager.GetInt("framecap");

            //Value of -1 uncaps the framerate
            if(framecap == 0 || framecap > 200) framecap = -1;

            Application.targetFrameRate = framecap;
        }
        else Application.targetFrameRate = -1;

#if !UNITY_WEBGL
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
#else
        mainCamera.allowMSAA = false;
#endif

        // ssaoFeature.SetActive(SettingsManager.GetBool("ssao"));

#if !UNITY_EDITOR
        float mainBloomStrength = SettingsManager.GetFloat("bloom");
        float backgroundBloomStrength = SettingsManager.GetFloat("backgroundbloom");

        mainBloom.active = mainBloomStrength > 0;
        backgroundBloom.active = backgroundBloomStrength > 0;

        mainBloom.intensity.value = mainBloomStrength * defaultBloomStrength;
        backgroundBloom.intensity.value = backgroundBloomStrength * defaultBackgroundBloomStrength;
#endif
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateGraphicsSettings;
        
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
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateGraphicsSettings;
    }


    private void Start()
    {
        UpdateGraphicsSettings();
    }
}