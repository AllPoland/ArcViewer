using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

public class HitSoundManager : MonoBehaviour
{
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

    [SerializeField] private AudioMixer hitSoundMixer;
    [SerializeField] private AudioClip defaultHitsound;


    public static void ScheduleHitsound(float noteTime, AudioSource noteSource)
    {
        noteSource.volume = 1f;

        if(RandomPitch)
        {
            noteSource.pitch = Random.Range(0.95f, 1.05f);
        }
        else noteSource.pitch = 1;

        if(Spatial)
        {
            noteSource.spatialBlend = 1f;
        }
        else noteSource.spatialBlend = 0;

        if(Spatial)
        {
            ScheduledSound existingSound = scheduledSounds.FirstOrDefault(x => Mathf.Abs(x.time - noteTime) <= 0.001);
            if(existingSound != null)
            {
                //This sound has already been scheduled, every source should have reduced volume
                //This keeps the aggregate volume the same while making spatial audio sound correct
                existingSound.source.volume *= 0.5f;
                noteSource.volume *= 0.5f;
                noteSource.pitch = existingSound.source.pitch;
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

        sound.source.enabled = true;
        sound.source.Stop();
        sound.source.clip = HitSound;

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
        if(currentTime > time + (source.clip.length / source.pitch) + 0.05)
        {
            //The time to play the sound has already passed
            Destroy();
            return;
        }

        float timeDifference = time - currentTime;
        if(HitSoundManager.DynamicPriority)
        {
            //Dynamically set sound priority so sounds don't get overridden by scheduled sounds
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

        if(currentTime > time - HitSoundManager.ScheduleBuffer && !source.isPlaying)
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