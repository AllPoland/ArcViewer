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
            float hitSoundVolume = SettingsManager.GetFloat("hitsoundvolume");
#if !UNITY_WEBGL || UNITY_EDITOR
            hitSoundManager.HitSoundVolume = hitSoundVolume;
            hitSoundManager.ChainVolume = SettingsManager.GetFloat("chainvolume") * hitSoundVolume;
#else
            float chainSoundVolume = SettingsManager.GetFloat("chainvolume");

            WebHitSoundController.SetHitSoundVolume(SettingsManager.GetFloat("hitsoundvolume"));
            WebHitSoundController.SetChainSoundVolume(SettingsManager.GetFloat("chainvolume"));

            bool hitSoundsOff = WebHitSoundController.CurrentHitSoundVolume < Mathf.Epsilon;
            bool chainSoundsOff = WebHitSoundController.CurrentChainSoundVolume < Mathf.Epsilon;
            if((hitSoundsOff && hitSoundVolume > Mathf.Epsilon) || (chainSoundsOff && chainSoundVolume > Mathf.Epsilon))
            {
                //Reschedule hitsounds if volume is going from 0 to greater than zero
                //This is necessary because web audio refuses to process audio with 0 volume
                HitSoundManager.ClearScheduledSounds();
                ObjectManager.Instance.RescheduleHitsounds(TimeManager.Playing);
            }

            WebHitSoundController.CurrentHitSoundVolume = hitSoundVolume;
            WebHitSoundController.CurrentChainSoundVolume = chainSoundVolume;
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