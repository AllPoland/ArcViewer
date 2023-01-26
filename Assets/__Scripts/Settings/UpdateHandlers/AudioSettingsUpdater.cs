using UnityEngine;

public class AudioSettingsUpdater : MonoBehaviour
{
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private HitSoundManager hitSoundManager;


    public void UpdateAudioSettings()
    {
        audioManager.MusicVolume = SettingsManager.GetFloat("musicvolume");
        hitSoundManager.HitSoundVolume = SettingsManager.GetFloat("hitsoundvolume");
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