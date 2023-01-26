using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

public class HitSoundManager : MonoBehaviour
{
    public static AudioClip HitSound;
    public static List<ScheduledSound> scheduledSounds = new List<ScheduledSound>();

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

        float timeDifference = noteTime - AudioManager.GetSongTime();

        sound.source.clip = HitSound;
        sound.source.PlayScheduled(AudioSettings.dspTime + timeDifference);

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


    public void Destroy()
    {
        source?.Stop();

        TimeManager.OnBeatChanged -= UpdateTime;
        parentList.Remove(this);
    }


    public void UpdateTime(float currentBeat)
    {
        if(source == null)
        {
            Destroy();
            return;
        }

        float currentTime = AudioManager.GetSongTime();
        if(!source.isPlaying)
        {
            Destroy();
        }
    }
}