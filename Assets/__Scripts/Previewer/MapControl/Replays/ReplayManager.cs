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
    public static string[] Modifiers = new string[0];

    public static bool LeftHandedMode => IsReplayMode && CurrentReplay.info.leftHanded;
    public static bool BatteryEnergy { get; private set; }

    public static bool Failed = false;
    public static float FailTime = 0f;
    public static bool HasFailed => Failed && TimeManager.CurrentTime >= FailTime;

    private static MapElementList<PlayerHeightEvent> playerHeightEvents = new MapElementList<PlayerHeightEvent>();


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

        Modifiers = CurrentReplay.info.modifiers.Split(',');
        BatteryEnergy = Modifiers.Any(x => x.Equals("BE", StringComparison.InvariantCultureIgnoreCase));

        OnReplayModeChanged?.Invoke(true);
        OnReplayUpdated?.Invoke(CurrentReplay);

        TimeManager.OnBeatChangedEarly += UpdateBeat;

        ReplayInfo info = CurrentReplay.info;
        Debug.Log($"Loaded replay for {info.songName}, {info.mode}, {info.difficulty}, played by {info.playerName}, with score {info.score}, and modifiers: {info.modifiers}");
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
        Modifiers = new string[0];

        Failed = false;
        FailTime = 0f;

        OnReplayModeChanged?.Invoke(false);
        OnReplayUpdated?.Invoke(null);

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
        Time = a.time;
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