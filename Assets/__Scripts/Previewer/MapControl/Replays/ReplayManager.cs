using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ReplayManager : MonoBehaviour
{
    public static bool IsReplayMode { get; private set; }
    public static Replay CurrentReplay { get; private set; }

    public static event Action<bool> OnReplayModeChanged;
    public static event Action<Replay> OnReplayUpdated;

    public static float PlayerHeight;

    private static MapElementList<PlayerHeightEvent> playerHeightEvents = new MapElementList<PlayerHeightEvent>();


    public static List<NoteEvent> GetNoteEventsAtTime(float time) => CurrentReplay.notes.FindAll(x => ObjectManager.CheckSameTime(x.spawnTime, time));


    public static int GetAccScoreFromCenterDistance(float centerDistance)
    {
        const int maxAccScore = 15;
        return Mathf.RoundToInt(maxAccScore * (1f - Mathf.Clamp01(centerDistance / 0.3f)));
    }


    public static int GetNoteScore(ScoringType type, float preSwingAmount, float postSwingAmount, float centerDistance)
    {
        const int preSwingValue = 70;
        const int postSwingValue = 30;

        if(type == ScoringType.ChainLink)
        {
            return 20;
        }
        
        if(type == ScoringType.ArcHead)
        {
            //Arc heads get post swing for free
            postSwingAmount = 1f;
        }
        else if(type == ScoringType.ArcTail)
        {
            //Arc tails get pre swing for free
            preSwingAmount = 1f;
        }
        else if(type == ScoringType.ChainHead)
        {
            //Chain heads don't get post swing points at all
            postSwingAmount = 0f;
        }

        int preSwingScore = Mathf.RoundToInt(Mathf.Clamp01(preSwingAmount) * preSwingValue);
        int postSwingScore = Mathf.RoundToInt(Mathf.Clamp01(postSwingAmount) * postSwingValue);
        return preSwingScore + postSwingScore + GetAccScoreFromCenterDistance(Mathf.Abs(centerDistance));
    }


    public static void SetReplay(Replay newReplay)
    {
        if(newReplay == null)
        {
            return;
        }

        newReplay.notes.OrderBy(x => x.spawnTime);
        newReplay.pauses.OrderBy(x => x.time);
        newReplay.walls.OrderBy(x => x.time);

        playerHeightEvents.Clear();
        for(int i = 0; i < newReplay.heights.Count; i++)
        {
            playerHeightEvents.Add(new PlayerHeightEvent(newReplay.heights[i]));
        }
        playerHeightEvents.SortElementsByBeat();

        IsReplayMode = true;
        CurrentReplay = newReplay;

        OnReplayModeChanged?.Invoke(true);
        OnReplayUpdated?.Invoke(CurrentReplay);

        TimeManager.OnBeatChangedEarly += UpdateBeat;
    }


    private static void UpdatePlayerHeight(float beat)
    {
        int lastHeightIndex = playerHeightEvents.GetLastIndex(TimeManager.CurrentTime, x => x.Time <= TimeManager.CurrentTime);
        PlayerHeight = lastHeightIndex >= 0
            ? playerHeightEvents[lastHeightIndex].Height
            : CurrentReplay.info.height;
    }


    private static void UpdateBeat(float beat)
    {
        UpdatePlayerHeight(beat);
    }


    private static void Reset()
    {
        IsReplayMode = false;
        CurrentReplay = null;
        PlayerHeight = ObjectManager.DefaultPlayerHeight;

        OnReplayModeChanged?.Invoke(false);

        TimeManager.OnBeatChangedEarly -= UpdateBeat;
    }


    private static void UpdateUIState(UIState newState)
    {
        if(newState == UIState.MapSelection)
        {
            Reset();
        }
    }


    private void Start()
    {
        UIStateManager.OnUIStateChanged += UpdateUIState;
        MapLoader.OnLoadingFailed += Reset;
    }
}


public class PlayerHeightEvent : MapElement
{
    public float Height;

    public PlayerHeightEvent(AutomaticHeight a)
    {
        Beat = TimeManager.BeatFromTime(a.time);
        Height = a.height;
    }
}


public enum ScoringType
{
    Ignore = 1,
    NoScore = 2,
    Note = 3,
    ArcHead = 4,
    ArcTail = 5,
    ChainHead = 6,
    ChainLink = 7
}