using UnityEngine;

public class AudioSettingsUpdater : MonoBehaviour
{
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private HitSoundManager hitSoundManager;
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private ObjectSettingsUpdater objectSettingsUpdater;


    public void UpdateAudioSettings(string setting)
    {
        bool allSettings = setting == "all";
        if(allSettings || setting == "musicvolume")
        {
            audioManager.MusicVolume = SettingsManager.GetFloat("musicvolume");
        }

        if(allSettings || setting == "hitsoundvolume" || setting == "chainvolume")
        {
            float hitsoundVolume = SettingsManager.GetFloat("hitsoundvolume");
            hitSoundManager.HitSoundVolume = hitsoundVolume;
            hitSoundManager.ChainVolume = SettingsManager.GetFloat("chainvolume") * hitsoundVolume;
        }

        if(allSettings || setting == "hitsound" || setting == "spatialhitsounds" || setting == "randomhitsoundpitch")
        {
            int hitsound = SettingsManager.GetInt("hitsound");
            Mathf.Clamp(hitsound, 0, hitSounds.Length - 1);
            AudioClip newSound = hitSounds[hitsound];

            HitSoundManager.HitSound = newSound;

            HitSoundManager.ClearScheduledSounds();
            ObjectManager.Instance.RescheduleHitsounds(TimeManager.Playing);
        }
    }
    

    private void Start()
    {
        SettingsManager.OnSettingsUpdated += UpdateAudioSettings;
        UpdateAudioSettings("all");
    }
}