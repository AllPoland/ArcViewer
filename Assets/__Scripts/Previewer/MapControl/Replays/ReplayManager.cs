using System;
using System.Linq;
using UnityEngine;

public class ReplayManager : MonoBehaviour
{
    public static bool IsReplayMode { get; private set; }
    public static Replay CurrentReplay { get; private set; }

    public static event Action<bool> OnReplayModeChanged;
    public static event Action<Replay> OnReplayUpdated;


    public static void SetReplay(Replay newReplay)
    {
        if(newReplay == null)
        {
            return;
        }

        newReplay.frames.OrderBy(x => x.time);
        newReplay.heights.OrderBy(x => x.time);
        newReplay.notes.OrderBy(x => x.eventTime);
        newReplay.pauses.OrderBy(x => x.time);
        newReplay.walls.OrderBy(x => x.time);

        IsReplayMode = true;
        CurrentReplay = newReplay;

        OnReplayModeChanged?.Invoke(true);
        OnReplayUpdated?.Invoke(CurrentReplay);
    }


    private void Reset()
    {
        IsReplayMode = false;
        CurrentReplay = null;

        OnReplayModeChanged?.Invoke(false);
    }


    private void UpdateUIState(UIState newState)
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