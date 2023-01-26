using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

public class HitSoundManager : MonoBehaviour
{
    public static AudioClip HitSound;
    public static List<ScheduledSound> scheduledSounds = new List<ScheduledSound>();

    public static bool randomPitch = false;
    public static float scheduleBuffer = 0.2f;
    public static bool dynamicPriority = true;

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

    [SerializeField] private AudioMixer hitSoundMixer;
    [SerializeField] private AudioClip defaultHitsound;


    public static void ScheduleHitsound(float noteTime, AudioSource noteSource)
    {
        if(scheduledSounds.Any(x => Mathf.Abs(x.time - noteTime) <= 0.001))
        {
            //This sound has already been scheduled, don't stack
            return;
        }

        ScheduledSound sound = new ScheduledSound
        {
            parentList = scheduledSounds,
            source = noteSource,
            time = noteTime
        };

        sound.source.clip = HitSound;

        if(randomPitch)
        {
            sound.source.pitch = Random.Range(0.95f, 1.05f);
        }
        else sound.source.pitch = 1;

        TimeManager.OnBeatChanged += sound.UpdateTime;
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


    private void Start()
    {
        TimeManager.OnPlayingChanged += UpdatePlaying;
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

    private bool scheduled;


    public void Destroy()
    {
        if(source != null)
        {
            source.Stop();
            source.enabled = false;
        }

        TimeManager.OnBeatChanged -= UpdateTime;
        parentList.Remove(this);
    }


    public void UpdateTime(float currentBeat)
    {
        if(source == null || !source.isActiveAndEnabled)
        {
            Destroy();
            return;
        }

        float currentTime = AudioManager.GetSongTime();
        if(currentTime > time + (source.clip.length / source.pitch))
        {
            //The time to play the sound has already passed
            Destroy();
            return;
        }

        float timeDifference = time - currentTime;
        if(HitSoundManager.dynamicPriority)
        {
            //Dynamically set sound priority so sounds don't get overridden by scheduled sounds
            float progress = timeDifference / HitSoundManager.scheduleBuffer;
            int priority = (int)(Mathf.Min(progress, 1) * 255) + 1;
            source.priority = priority;
        }

        if(currentTime > time - HitSoundManager.scheduleBuffer && !source.isPlaying)
        {
            if(!scheduled)
            {
                //Audio hasn't been scheduled but it should be
                source.PlayScheduled(AudioSettings.dspTime + timeDifference);
                scheduled = true;
            }
        }
    }
}