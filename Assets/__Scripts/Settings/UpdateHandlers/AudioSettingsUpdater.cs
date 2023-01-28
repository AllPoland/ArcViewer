using UnityEngine;

public class AudioSettingsUpdater : MonoBehaviour
{
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private HitSoundManager hitSoundManager;
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private ObjectSettingsUpdater objectSettingsUpdater;


    public void UpdateAudioSettings()
    {
        audioManager.MusicVolume = SettingsManager.GetFloat("musicvolume");

        float hitsoundVolume = SettingsManager.GetFloat("hitsoundvolume");

        hitSoundManager.HitSoundVolume = hitsoundVolume;
        hitSoundManager.ChainVolume = SettingsManager.GetFloat("chainvolume") * hitsoundVolume;
        HitSoundManager.RandomPitch = SettingsManager.GetBool("randomhitsoundpitch");
        HitSoundManager.Spatial = SettingsManager.GetBool("spatialhitsounds");
        HitSoundManager.ScheduleBuffer = SettingsManager.GetFloat("hitsoundbuffer");
        HitSoundManager.DynamicPriority = SettingsManager.GetBool("dynamicsoundpriority");

        int hitsound = SettingsManager.GetInt("hitsound");
        Mathf.Clamp(hitsound, 0, hitSounds.Length - 1);
        //I love making good variable names
        HitSoundManager.HitSound = hitSounds[hitsound];

        //Extremely yucky spaghetti-type fix for object hitsounds not updating correctly
        //I need to make sure objects update after hitsound has been set
        HitSoundManager.ClearScheduledSounds();
        objectSettingsUpdater.UpdateObjectSettings();
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