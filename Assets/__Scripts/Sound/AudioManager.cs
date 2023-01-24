using UnityEngine;

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

    private AudioSource musicSource;


    public static float GetMapTime()
    {
        return TimeManager.CurrentTime + BeatmapManager.Info._songTimeOffset;
    }


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
            float mapTime = GetMapTime();
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

        musicSource.clip = newClip;
        TimeManager.SongLength = GetSongLength() - BeatmapManager.Info._songTimeOffset;
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