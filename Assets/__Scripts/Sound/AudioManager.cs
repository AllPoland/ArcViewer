using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public bool Correct = true;
    public float MaxCorrection = 0.005f;
    public float CorrectionRangeOverride = 0.05f;

    private AudioClip _musicClip;
    public AudioClip MusicClip
    {
        get
        {
            return _musicClip;
        }
        set
        {
            _musicClip = value;
            UpdateAudioClip(value);
        }
    }

    private AudioSource source;


    public void UpdateAudioClip(AudioClip newClip)
    {
        if(newClip == null)
        {
            return;
        }

        source.clip = newClip;
        TimeManager.SongLength = newClip.length - BeatmapManager.Info._songTimeOffset;
    }


    public void UpdatePlaying(bool playing)
    {
        if(playing)
        {
            float songTime = GetSongTime();
            if(songTime <= 0 || songTime > source.clip.length)
            {
                return;
            }
            
            source.time = songTime;
            source.Play();
        }
        else
        {
            source.Stop();
        }
    }


    public void CorrectTiming()
    {
        if(!TimeManager.Playing || !Correct)
        {
            TimeManager.Correction = 0f;
            return;
        }
        if(source.time <= 0 || source.time >= source.clip.length)
        {
            UpdatePlaying(TimeManager.Playing);
            return;
        }

        float disc = source.time - GetSongTime();
        float correction = disc > CorrectionRangeOverride ? disc : Mathf.Min(disc, MaxCorrection) ;
        TimeManager.Correction = correction;
        //Debug.Log($"Correcting by {correction} seconds.");
    }


    public float GetSongTime()
    {
        return TimeManager.CurrentTime + BeatmapManager.Info._songTimeOffset;
    }


    private void Update()
    {
        CorrectTiming();
    }


    private void Awake()
    {
        source = GetComponent<AudioSource>();
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