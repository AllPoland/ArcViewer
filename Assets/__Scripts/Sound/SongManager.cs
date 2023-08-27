using System;
using UnityEngine;
using UnityEngine.Audio;

public class SongManager : MonoBehaviour
{
    public static SongManager Instance { get; private set; }

#if !UNITY_WEBGL || UNITY_EDITOR
    private AudioClip _musicClip;
    public AudioClip MusicClip
#else
    private WebAudioClip _musicClip;
    public WebAudioClip MusicClip
#endif
    {
        get => _musicClip;
        set
        {
            DestroyClip();
            _musicClip = value;

            //Make sure the clip has the correct speed
            UpdateSpeed(TimeSyncHandler.TimeScale);

            float songTimeOffset = BeatmapManager.Info._songTimeOffset;
            if(songTimeOffset != 0)
            {
#if !UNITY_WEBGL || UNITY_EDITOR
                ApplySongTimeOffset(ref _musicClip);
#else
                _musicClip.SetOffset(songTimeOffset);
#endif
                ErrorHandler.Instance.ShowPopup(ErrorType.Warning, "Song Time Offset is depreciated!");
            }

#if !UNITY_WEBGL || UNITY_EDITOR
            Instance.musicSource.clip = _musicClip;
#endif
        }
    }

    public float MusicVolume
    {
        set
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if(value.Approximately(0f))
            {
                //Can't set this to 0 because Log breaks
                value = 0.0001f;
            }
            //Logarithmic scaling makes volume slider feel more natural to the user
            musicMixer.SetFloat("Volume", Mathf.Log10(value) * 20);
#else
            WebAudioController.SetVolume(value);
#endif
        }
    }

    [SerializeField] private AudioMixer musicMixer;

#if !UNITY_WEBGL || UNITY_EDITOR
    private AudioSource musicSource;
#endif


    public void DestroyClip()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        musicSource.Stop();
        if(musicSource.clip != null)
        {
            musicSource.clip.UnloadAudioData();
            Destroy(musicSource.clip);
            musicSource.clip = null;
        }

        if(_musicClip != null)
        {
            _musicClip.UnloadAudioData();
            Destroy(_musicClip);
            _musicClip = null;
        }
#else
        _musicClip?.Dispose();
#endif
    }


    public static float GetSongTime()
    {
        if(Instance.MusicClip == null) return 0;
#if !UNITY_WEBGL || UNITY_EDITOR
        return (float)(Instance.musicSource.timeSamples) / Instance.MusicClip.frequency;
#else
        return Instance.MusicClip.Time;
#endif
    }


    public static float GetSongLength()
    {
        if(Instance.MusicClip == null) return 0;
#if !UNITY_WEBGL || UNITY_EDITOR
        return (float)Instance.MusicClip.samples / Instance.MusicClip.frequency;
#else
        return Instance.MusicClip.Length;
#endif
    }


    public void PlaySong(float time)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        musicSource.time = time;
        musicSource.Play();
#else
        MusicClip?.Play(time);
#endif
    }


    public void StopSong()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        musicSource.Stop();
#else
        MusicClip?.Stop();
#endif
    }


    public void UpdatePlaying(bool playing)
    {
        if(playing)
        {
            float mapTime = TimeManager.CurrentTime;
            if(mapTime < 0 || mapTime >= GetSongLength())
            {
                StopSong();
                TimeManager.SetPlaying(false);
                return;
            }

            PlaySong(mapTime);
        }
        else StopSong();
    }


    public void UpdateSpeed(float speed)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        musicSource.pitch = speed;
#else
        MusicClip?.SetSpeed(speed);
#endif
    }


#if !UNITY_WEBGL || UNITY_EDITOR
    private void ApplySongTimeOffset(ref AudioClip clip)
    {
        //Take songTimeOffset into account by adjusting newClip data forward/backward
        //I *definitely* didn't steal this from ChroMapper (Caeden don't kill me)

        float songTimeOffset = BeatmapManager.Info._songTimeOffset;

        //Guaranteed to always be an integer multiple of the number of channels
        int songTimeOffsetSamples = Mathf.CeilToInt(songTimeOffset * clip.frequency) * clip.channels;
        float[] samples = new float[clip.samples * clip.channels];

        clip.GetData(samples, 0);

        //Negative offset: Shift existing data forward, fill in beginning blank with 0s
        if(songTimeOffsetSamples < 0)
        {
            Array.Resize(ref samples, samples.Length - songTimeOffsetSamples);

            for(int i = samples.Length - 1; i >= 0; i--)
            {
                int shiftIndex = i + songTimeOffsetSamples;

                samples[i] = shiftIndex < 0 ? 0 : samples[shiftIndex];
            }
        }
        //Positive offset: Shift existing data backward, cut off ending blank
        else
        {
            for(int i = 0; i < samples.Length; i++)
            {
                int shiftIndex = i + songTimeOffsetSamples;

                samples[i] = shiftIndex >= samples.Length ? 0 : samples[shiftIndex];
            }

            // Bit of a hacky workaround, since you can't create an AudioClip with 0 length,
            // This just sets a minimum of 4096 samples per channel
            Array.Resize(ref samples, Math.Max(samples.Length - songTimeOffsetSamples, clip.channels * 4096));
        }

        Destroy(clip);

        // Create a new AudioClip because apparently you can't change the length of an existing one
        clip = AudioClip.Create(clip.name, samples.Length / clip.channels, clip.channels, clip.frequency, false);
        clip.SetData(samples, 0);
    }


    private void Update()
    {
        if(TimeManager.Playing && !musicSource.isPlaying)
        {
            //The song has finished playing, so stop the map
            TimeManager.SetPlaying(false);
            TimeManager.CurrentTime = GetSongLength();
        }
    }


    private void Awake()
    {
        musicSource = GetComponent<AudioSource>();
    }
#endif


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
        TimeSyncHandler.OnTimeScaleChanged += UpdateSpeed;
    }
}