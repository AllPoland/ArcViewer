using System.Linq;
using UnityEngine;

public class AudioSettingsUpdater : MonoBehaviour
{
    [SerializeField] private HitSoundManager hitSoundManager;
    [SerializeField] private ObjectSettingsUpdater objectSettingsUpdater;

    [Space]
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private AudioClip[] badHitSounds;

    private string[] hitSoundSettings = new string[]
    {
        "hitsound",
        "spatialhitsounds",
        "randomhitsoundpitch",
        "usebadhitsound",
        "badhitsound",
        "mutemisses"
    };


    public void UpdateAudioSettings(string setting)
    {
        bool allSettings = setting == "all";
        if(allSettings || setting == "musicvolume")
        {
            SongManager.Instance.MusicVolume = SettingsManager.GetFloat("musicvolume");
        }

        if(allSettings || setting == "hitsoundvolume" || setting == "chainvolume")
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            float hitsoundVolume = SettingsManager.GetFloat("hitsoundvolume");
            hitSoundManager.HitSoundVolume = hitsoundVolume;
            hitSoundManager.ChainVolume = SettingsManager.GetFloat("chainvolume") * hitsoundVolume;
#else
            WebHitSoundController.SetHitSoundVolume(SettingsManager.GetFloat("hitsoundvolume"));
            WebHitSoundController.SetChainSoundVolume(SettingsManager.GetFloat("chainvolume"));
#endif
        }

        if(allSettings || hitSoundSettings.Contains(setting))
        {
            int hitsound = SettingsManager.GetInt("hitsound");
            hitsound = Mathf.Clamp(hitsound, 0, hitSounds.Length - 1);

            int badHitsound = SettingsManager.GetInt("badhitsound");
            badHitsound = Mathf.Clamp(badHitsound, 0, badHitSounds.Length - 1);

#if !UNITY_WEBGL || UNITY_EDITOR
            HitSoundManager.HitSound = hitSounds[hitsound];
            HitSoundManager.BadHitSound = badHitSounds[badHitsound];
#else
            WebHitSoundController.SetHitSound(hitsound);
            WebHitSoundController.SetBadHitSound(badHitsound);
#endif

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