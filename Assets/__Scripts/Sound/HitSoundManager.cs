using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

public class HitSoundManager : MonoBehaviour
{
    public const float SoundOffset = -0.185f;

    public static AudioClip HitSound;
    public static List<ScheduledSound> scheduledSounds = new List<ScheduledSound>();

    public static bool RandomPitch = false;
    public static bool Spatial = false;
    public static float ScheduleBuffer = 0.2f;
    public static bool DynamicPriority = true;

    public float HitSoundVolume
    {
        set
        {
            if(value == 0)
            {
                value = 0.0001f;
            }
            //Logarithmic scaling makes volume slider feel more natural to the user
            hitSoundMixer.SetFloat("Volume", Mathf.Log10(value) * 20);
        }
    }

    public float ChainVolume
    {
        set
        {
            if(value == 0)
            {
                value = 0.0001f;
            }
            //Logarithmic scaling makes volume slider feel more natural to the user
            hitSoundMixer.SetFloat("ChainVolume", Mathf.Log10(value) * 20);
        }
    }

    [SerializeField] private AudioMixer hitSoundMixer;
    [SerializeField] private AudioClip defaultHitsound;


    public static void ScheduleHitsound(float noteTime, AudioSource noteSource)
    {
        noteSource.enabled = true;
        noteSource.Stop();
        noteSource.clip = HitSound;
        noteSource.volume = 1f;

        if(RandomPitch)
        {
            noteSource.pitch = Random.Range(0.95f, 1.05f);
        }
        else noteSource.pitch = 1;

#if !UNITY_WEBGL || UNITY_EDITOR
        if(Spatial)
        {
            //A bit less than full spacial blend because I think having the sounds way in one ear is weird
            noteSource.spatialBlend = 0.8f;
        }
        else noteSource.spatialBlend = 0;
#endif

        if(Spatial)
        {
            //Findall my behated
            List<ScheduledSound> existingSounds = scheduledSounds.FindAll(x => Mathf.Abs(x.time - noteTime) <= 0.001);
            if(existingSounds.Count > 0)
            {
                //This sound has already been scheduled, every source should match pitch and have reduced volume
                const float volumeFalloff = 0.6f;
                foreach(ScheduledSound existingSound in existingSounds)
                {
                    existingSound.source.volume *= volumeFalloff;
                }
                noteSource.volume *= volumeFalloff;

                noteSource.pitch = existingSounds[0].source.pitch;
            }
        }
        else if(scheduledSounds.Any(x => Mathf.Abs(x.time - noteTime) <= 0.001))
        {
            //This sound has already been scheduled and we aren't using spatial audio, so sounds shouldn't stack
            return;
        }

        ScheduledSound sound = new ScheduledSound
        {
            parentList = scheduledSounds,
            source = noteSource,
            time = noteTime
        };
        scheduledSounds.Add(sound);
    }


    public static void ClearScheduledSounds()
    {
        for(int i = scheduledSounds.Count - 1; i >= 0; i--)
        {
            scheduledSounds[i].Destroy();
        }
    }


    public void UpdatePlaying(bool playing)
    {
        if(!playing)
        {
            ClearScheduledSounds();
        }
    }


    public void UpdateTimeScale(float newScale)
    {
        for(int i = scheduledSounds.Count - 1; i >= 0; i--)
        {
            ScheduledSound sound = scheduledSounds[i];

            //If the sound is already scheduled, try to reschedule it with the correct timing
            if(sound.scheduled)
            {
                sound.source.Stop();
                sound.scheduled = false;

                sound.UpdateTime();
            }
        }
    }


    private void Update()
    {
        for(int i = scheduledSounds.Count - 1; i >= 0; i--)
        {
            //Update each sound's priority and queue and such
            scheduledSounds[i].UpdateTime();
        }
    }


    private void Start()
    {
        TimeManager.OnPlayingChanged += UpdatePlaying;
        TimeSyncHandler.OnTimeScaleChanged += UpdateTimeScale;
    }


    private void Awake()
    {
        HitSound = defaultHitsound;
    }
}


public class ScheduledSound
{
    public List<ScheduledSound> parentList;
    public AudioSource source;
    public float time;
    public bool scheduled;


    public void Destroy()
    {
        if(source != null)
        {
            source.Stop();
        }

        parentList.Remove(this);
    }


    public void UpdateTime()
    {
        if(source == null || !source.isActiveAndEnabled)
        {
            Destroy();
            return;
        }

        float currentTime = AudioManager.GetSongTime();

        //Account for time scale and sound offset
        float timeDifference = (time - currentTime) / TimeSyncHandler.TimeScale;
        float scheduleIn = timeDifference + HitSoundManager.SoundOffset;

        if(!scheduled && scheduleIn <= 0)
        {
            //The sound should already be playing by this point
            //Trying to schedule now would just make it off-time and wouldn't be worth it
            Destroy();
            return;
        }

        if(HitSoundManager.DynamicPriority)
        {
            //Dynamically set sound priority so playing sounds don't get overridden by scheduled sounds
            //Thanks galx for making this code
            if(currentTime - time > 0)
            {
                const float priorityFalloff = 384;
                source.priority = Mathf.Clamp(1 + Mathf.RoundToInt((float)(currentTime - time) * priorityFalloff), 1, 254);
            }
            else
            {
                const float priorityRampup = 192;
                source.priority = Mathf.Clamp(1 + Mathf.RoundToInt((float)(time - currentTime) * priorityRampup), 1, 255);
            }
        }
        else source.priority = 100;

        if(!scheduled && !source.isPlaying && scheduleIn <= HitSoundManager.ScheduleBuffer)
        {
            //Audio hasn't been scheduled but it should be
            source.PlayScheduled(AudioSettings.dspTime + scheduleIn);
            scheduled = true;
        }
    }
}