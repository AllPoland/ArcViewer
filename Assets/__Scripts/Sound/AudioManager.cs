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
    private float songTimeOffset;
    private float scheduleMusicTime = -1;


    public static float GetSongTime()
    {
        if(Instance?.MusicClip == null) return 0;

        //Music has offset to burn through before it starts playing, surrender control to timemanager
        if(Instance.scheduleMusicTime > 0) return TimeManager.CurrentTime;

        return ((float)(Instance.musicSource.timeSamples) / Instance.MusicClip.frequency) + Instance.songTimeOffset;
    }


    public static float GetSongLength()
    {
        if(Instance?.MusicClip == null) return 0;

        return (Instance.MusicClip.samples / Instance.MusicClip.frequency) + Instance.songTimeOffset;
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
            
            float targetTime = mapTime - songTimeOffset;
            Debug.Log(targetTime);
            if(targetTime < 0)
            {
                //Need to wait for song time offset
                scheduleMusicTime = songTimeOffset;
                musicSource.Stop();
                return;
            }

            musicSource.time = targetTime;
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
        songTimeOffset = BeatmapManager.Info._songTimeOffset;
    }


    private void Update()
    {
        if(scheduleMusicTime > 0 && TimeManager.CurrentTime >= scheduleMusicTime)
        {
            scheduleMusicTime = -1;
            UpdatePlaying(true);
        }
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