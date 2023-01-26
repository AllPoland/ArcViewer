using System;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioClip _musicClip;
    public AudioClip MusicClip
    {
        get => _musicClip;
        set
        {
            _musicClip = value;
            UpdateAudioClip(value);
        }
    }

    public float musicVolume
    {
        set
        {
            if(value == 0)
            {
                musicMixer.SetFloat("Volume", -80);
                return;
            }
            //Logarithmic scaling makes volume slider feel more natural to the user
            musicMixer.SetFloat("Volume", Mathf.Log10(value) * 20);
        }
    }

    [SerializeField] private AudioMixer musicMixer;

    private AudioSource musicSource;


    public static float GetSongTime()
    {
        if(Instance?.MusicClip == null) return 0;

        return (float)(Instance.musicSource.timeSamples) / Instance.MusicClip.frequency;
    }


    public static float GetSongLength()
    {
        if(Instance?.MusicClip == null) return 0;

        return Instance.MusicClip.samples / Instance.MusicClip.frequency;
    }


    public void UpdatePlaying(bool playing)
    {
        if(playing)
        {
            float mapTime = TimeManager.CurrentTime;
            if(mapTime < 0 || mapTime > GetSongLength())
            {
                return;
            }

            musicSource.time = mapTime;
            musicSource.Play();
        }
        else
        {
            musicSource.Stop();
        }
    }


    private void UpdateAudioClip(AudioClip newClip)
    {
        if(newClip == null)
        {
            return;
        }

        float songTimeOffset = BeatmapManager.Info._songTimeOffset;

        //I *definitely* didn't steal this from ChroMapper (Caeden don't kill me)
        if(songTimeOffset != 0)
        {
            // Take songTimeOffset into account by adjusting newClip data forward/backward

            // Guaranteed to always be an integer multiple of the number of channels
            int songTimeOffsetSamples = Mathf.CeilToInt(songTimeOffset * newClip.frequency) * newClip.channels;
            float[] samples = new float[newClip.samples * newClip.channels];

            newClip.GetData(samples, 0);

            // Negative offset: Shift existing data forward, fill in beginning blank with 0s
            if(songTimeOffsetSamples < 0)
            {
                Array.Resize(ref samples, samples.Length - songTimeOffsetSamples);

                for(int i = samples.Length - 1; i >= 0; i--)
                {
                    int shiftIndex = i + songTimeOffsetSamples;

                    samples[i] = shiftIndex < 0 ? 0 : samples[shiftIndex];
                }
            }
            // Positive offset: Shift existing data backward, cut off ending blank
            else
            {
                for(int i = 0; i < samples.Length; i++)
                {
                    int shiftIndex = i + songTimeOffsetSamples;

                    samples[i] = shiftIndex >= samples.Length ? 0 : samples[shiftIndex];
                }

                // Bit of a hacky workaround, since you can't create an AudioClip with 0 length,
                // This just sets a minimum of 4096 samples per channel
                Array.Resize(ref samples, Math.Max(samples.Length - songTimeOffsetSamples, newClip.channels * 4096));
            }

            // Create a new AudioClip because apparently you can't change the length of an existing one
            newClip = AudioClip.Create(newClip.name, samples.Length / newClip.channels, newClip.channels, newClip.frequency, false);
            newClip.SetData(samples, 0);
        }

        musicSource.clip = newClip;
    }


    private void Awake()
    {
        musicSource = GetComponent<AudioSource>();
    }


    private void OnEnable()
    {
        if(Instance && Instance != this)
        {
            Debug.Log("Duplicate AudioManager in the scene.");
            this.enabled = false;
        }
        else Instance = this;
    }


    private void Start()
    {
        TimeManager.OnPlayingChanged += UpdatePlaying;
    }
}