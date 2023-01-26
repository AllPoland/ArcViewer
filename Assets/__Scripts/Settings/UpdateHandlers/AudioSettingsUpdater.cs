using UnityEngine;

public class AudioSettingsUpdater : MonoBehaviour
{
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private HitSoundManager hitSoundManager;
    [SerializeField] private AudioClip[] hitSounds;


    public void UpdateAudioSettings()
    {
        audioManager.MusicVolume = SettingsManager.GetFloat("musicvolume");

        hitSoundManager.HitSoundVolume = SettingsManager.GetFloat("hitsoundvolume");
        HitSoundManager.randomPitch = SettingsManager.GetBool("randomhitsoundpitch");
        HitSoundManager.scheduleBuffer = SettingsManager.GetFloat("hitsoundbuffer");
        HitSoundManager.dynamicPriority = SettingsManager.GetBool("dynamicsoundpriority");

        int hitsound = SettingsManager.GetInt("hitsound");
        Mathf.Clamp(hitsound, 0, hitSounds.Length - 1);
        //I love making good variable names
        HitSoundManager.HitSound = hitSounds[hitsound];
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