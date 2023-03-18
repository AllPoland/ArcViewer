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
        get => _time;
        
        set
        {
            if(value >= SongLength)
            {
                _time = SongLength;
                _beat = BeatFromTime(_time);

                SetPlaying(false);
                OnBeatChanged?.Invoke(CurrentBeat);
                return;
            }
            else if(value < 0)
            {
                value = 0;
            }

            _beat = BeatFromTime(value);
            _time = value;
            OnBeatChanged?.Invoke(CurrentBeat);
        }
    }

    private static float _beat = 0;
    public static float CurrentBeat
    {
        get => _beat;
        
        private set
        {
            _time = TimeFromBeat(value);
            _beat = value;
            OnBeatChanged?.Invoke(_beat);
        }
    }


    public static float CurrentBPM
    {
        get
        {
            if(BpmChanges.Count == 0)
            {
                return BPM;
            }

            BpmChange lastChange = BpmChanges.FindLast(x => x.Beat < CurrentBeat);
            return lastChange.BPM;
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
        if(bpm <= 0) return 0;
        return beat / bpm * 60;
    }


    public static float RawBeatFromTime(float time, float bpm)
    {
        if(bpm <= 0) return 0;
        return time / 60 * bpm;
    }


    public static float Progress
    {
        get => SongLength <= 0 ? 0 : CurrentTime / SongLength;

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
        if(ForcePause)
        {
            newPlaying = false;
        }

        Playing = newPlaying && !ForcePause;
        if(Playing && Progress >= 1)
        {
            CurrentTime = 0;
        }

        OnPlayingChanged?.Invoke(Playing);
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

        List<BeatmapBpmEvent> bpmEvents = new List<BeatmapBpmEvent>();
        bpmEvents.AddRange(diff.beatmapDifficulty.bpmEvents);
        //Events must be ordered by beat for this to work (they almost always are but just gotta be safe)
        bpmEvents = bpmEvents.OrderBy(x => x.b).ToList();

        //Calculate the time of each change and populate the bpmChanges list
        float currentTime = 0;
        float lastBeat = 0;
        float lastBpm = BPM;
        foreach(BeatmapBpmEvent bpmEvent in bpmEvents)
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