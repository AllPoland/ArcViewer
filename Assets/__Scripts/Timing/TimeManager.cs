using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    public float BPM = 120;
    public float SongLength = 0;
    public bool ForcePause;
    public float Correction = 0;

    private float _time = 0;
    public float CurrentTime
    {
        get
        {
            return _time;
        }
        set
        {
            float set = value;
            if(set >= SongLength)
            {
                Debug.Log("Reached the end of the song.");
                SetPlaying(false);
                set = SongLength;
            }
            else if(set < 0)
            {
                Debug.LogWarning("Time is less than 0!");
                set = 0;
            }

            _beat = BeatFromTime(set);
            _time = set;
            OnBeatChanged?.Invoke(CurrentBeat);
        }
    }

    private float _beat = 0;
    public float CurrentBeat
    {
        get
        {
            return _beat;
        }
        private set
        {
            _time = TimeFromBeat(value);
            _beat = value;
            OnBeatChanged?.Invoke(_beat);
        }
    }

    public float Progress
    {
        get
        {
            return CurrentTime / SongLength;
        }
        set
        {
            if(value > 1)
            {
                value = 1;
            }

            CurrentTime = SongLength * value;
        }
    }

    public bool Playing { get; private set; }
    public void SetPlaying(bool newPlaying)
    {
        if(newPlaying == Playing)
        {
            return;
        }
        if(ForcePause)
        {
            newPlaying = false;
        }

        OnPlayingChanged?.Invoke(newPlaying);
        Playing = newPlaying;
    }

    public void TogglePlaying()
    {
        SetPlaying(!Playing);
    }

    public delegate void FloatDelegate(float value);
    public event FloatDelegate OnBeatChanged;

    public delegate void BoolDelegate(bool value);
    public event BoolDelegate OnPlayingChanged;


    public void UpdateInfo(BeatmapInfo info)
    {
        BPM = info._beatsPerMinute;
        OnBeatChanged?.Invoke(CurrentBeat);
    }


    public void UpdateUIState(UIState newState)
    {
        SetPlaying(false);
        CurrentTime = 0f;
    }


    public static float TimeFromBeat(float beat)
    {
        return beat / Instance.BPM * 60;
    }


    public static float BeatFromTime(float time)
    {
        return time / 60 * Instance.BPM;
    }


    private void Update()
    {
        if(Playing)
        {
            CurrentTime += Time.deltaTime + Correction;
        }
    }


    private void Start()
    {
        BeatmapManager.OnBeatmapInfoChanged += UpdateInfo;
        UIStateManager.OnUIStateChanged += UpdateUIState;
    }


    private void OnEnable()
    {
        if(Instance && Instance != this)
        {
            Debug.Log("Duplicate TimeManager in scene.");
            this.enabled = false;
        }
        else Instance = this;
    }


    private void OnDisable()
    {
        if(Instance == this)
        {
            Instance = null;
        }

        BeatmapManager.OnBeatmapInfoChanged -= UpdateInfo;
    }
}