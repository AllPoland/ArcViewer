using UnityEngine;

public class AudioSettingsUpdater : MonoBehaviour
{
    [SerializeField] private AudioManager audioManager;


    public void UpdateAudioSettings()
    {
        audioManager.musicVolume = SettingsManager.GetFloat("musicvolume");
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateAudioSettings;
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateAudioSettings;
    }
    

    private void Start()
    {
        UpdateAudioSettings();
    }
}