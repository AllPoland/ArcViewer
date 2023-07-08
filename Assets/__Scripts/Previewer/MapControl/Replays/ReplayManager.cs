using System;
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


    public static void SetReplay(Replay newReplay)
    {
        if(newReplay == null)
        {
            return;
        }

        newReplay.notes.OrderBy(x => x.eventTime);
        newReplay.pauses.OrderBy(x => x.time);
        newReplay.walls.OrderBy(x => x.time);

        playerHeightEvents.Clear();
        for(int i = 0; i < newReplay.heights.Count; i++)
        {
            playerHeightEvents.Add(new PlayerHeightEvent(newReplay.heights[i]));
        }

        IsReplayMode = true;
        CurrentReplay = newReplay;

        OnReplayModeChanged?.Invoke(true);
        OnReplayUpdated?.Invoke(CurrentReplay);

        TimeManager.OnBeatChanged += UpdateBeat;
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

        TimeManager.OnBeatChanged -= UpdateBeat;
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