using UnityEngine;

public class ReplayManager : MonoBehaviour
{
    public static bool IsReplayMode { get; private set; }
    public static Replay CurrentReplay { get; private set; }


    public static void SetReplay(Replay newReplay)
    {
        if(newReplay != null)
        {
            IsReplayMode = true;
            CurrentReplay = newReplay;
        }
    }


    private void Reset()
    {
        IsReplayMode = false;
        CurrentReplay = null;
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