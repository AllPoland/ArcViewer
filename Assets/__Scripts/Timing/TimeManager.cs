using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static float BPM = 120;
    public static bool ForcePause;
    public static float TimeScale = 1f;

    public static List<BpmChange> BpmChanges = new List<BpmChange>();

    public static event Action<float> OnBeatChanged;
    public static event Action<bool> OnPlayingChanged;

    private static float SongLength => AudioManager.GetSongLength();
    private static float _time = 0;
    public static float CurrentTime
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
                if(SongLength > 0)
                {
                    Debug.Log("Reached the end of the song.");
                }
                
                SetPlaying(false);
                set = SongLength;
            }
            else if(set < 0)
            {
                set = 0;
            }

            _beat = BeatFromTime(set);
            _time = set;
            OnBeatChanged?.Invoke(CurrentBeat);
        }
    }

    private static float _beat = 0;
    public static float CurrentBeat
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


    public static float TimeFromBeat(float beat)
    {
        if(BpmChanges.Count == 0)
        {
            return RawTimeFromBeat(beat, BPM);
        }

        BpmChange lastChange = BpmChanges.FindLast(x => x.Beat < beat);
        return lastChange.Time + RawTimeFromBeat(beat - lastChange.Beat, lastChange.BPM);
    }


    public static float BeatFromTime(float time)
    {
        if(BpmChanges.Count == 0)
        {
            return RawBeatFromTime(time, BPM);
        }

        BpmChange lastChange = BpmChanges.FindLast(x => x.Time < time);
        return lastChange.Beat + RawBeatFromTime(time - lastChange.Time, lastChange.BPM);
    }


    public static float RawTimeFromBeat(float beat, float bpm)
    {
        return beat / bpm * 60;
    }


    public static float RawBeatFromTime(float time, float bpm)
    {
        return time / 60 * bpm;
    }


    public static float Progress
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

    public static bool Playing { get; private set; }
    public static void SetPlaying(bool newPlaying)
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

    public static void TogglePlaying()
    {
        SetPlaying(!Playing);
    }


    public void UpdateInfo(BeatmapInfo info)
    {
        BPM = info._beatsPerMinute;
        OnBeatChanged?.Invoke(CurrentBeat);
    }


    public void UpdateDiff(Difficulty diff)
    {
        BpmChanges.Clear();

        List<BpmEvent> bpmEvents = new List<BpmEvent>();
        bpmEvents.AddRange(diff.beatmapDifficulty.bpmEvents);
        //Events must be ordered by beat for this to work (they almost always are but just gotta be safe)
        bpmEvents = bpmEvents.OrderBy(x => x.b).ToList();

        //Calculate the time of each change and populate the bpmChanges list
        float currentTime = 0;
        float lastBeat = 0;
        float lastBpm = BPM;
        foreach(BpmEvent bpmEvent in bpmEvents)
        {
            currentTime += RawTimeFromBeat(bpmEvent.b - lastBeat, lastBpm);

            BpmChange bpmChange = new BpmChange
            {
                Beat = bpmEvent.b,
                Time = currentTime,
                BPM = bpmEvent.m
            };
            BpmChanges.Add(bpmChange);

            lastBeat = bpmEvent.b;
            lastBpm = bpmEvent.m;
        }
    }


    public void UpdateUIState(UIState newState)
    {
        SetPlaying(false);
        CurrentTime = 0f;
    }


    private void Update()
    {
        if(Playing)
        {
            CurrentTime += Time.deltaTime * TimeScale;
        }
    }


    private void OnEnable()
    {
        BeatmapManager.OnBeatmapInfoChanged += UpdateInfo;
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateDiff;
        UIStateManager.OnUIStateChanged += UpdateUIState;
    }


    private void OnDisable()
    {
        BeatmapManager.OnBeatmapInfoChanged -= UpdateInfo;
        BeatmapManager.OnBeatmapDifficultyChanged -= UpdateDiff;
        UIStateManager.OnUIStateChanged -= UpdateUIState;
    }
}


public struct BpmChange
{
    public float Beat;
    public float Time;
    public float BPM;
}