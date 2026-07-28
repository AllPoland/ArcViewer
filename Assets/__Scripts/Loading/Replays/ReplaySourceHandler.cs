using UnityEngine;

public static class ReplaySourceHandler
{
    public static ReplayStreamingSocket Stream { get; private set; }
    public static bool IsStreaming { get; private set; }

    public static float StreamDelay => Mathf.Clamp(SettingsManager.GetFloat("streamdelay"), 0.1f, 10f);
    public static float StreamPosition => IsStreaming ? Stream.StreamTime - StreamDelay : Mathf.Infinity;


    public static void SetLiveTimeAndPlay()
    {
        TimeManager.SetPlaying(false);
        TimeManager.CurrentTime = StreamPosition;
        TimeManager.IsLivePosition = true;
        TimeManager.SetPlaying(true);
    }


    private static void UpdateUIState(UIState newState)
    {
        // Avoid resetting when the map loader is currently loading
        // This is because we go back to map selection when loading a new map for the stream
        if (newState == UIState.MapSelection && !MapLoader.Loading)
        {
            Reset();
        }
    }


    private static void UpdateDifficulty(Difficulty newDifficulty)
    {
        SetLiveTimeAndPlay();
    }


    public static void HandleStreamClosed(ReplayStreamingSocket closedStream)
    {
        if (closedStream != Stream)
        {
            return;
        }

        // Simply doing this *should* reset all state everywhere
        // (I don't really remember all this shit that well)
        UIStateManager.OnUIStateChanged -= UpdateUIState;
        UIStateManager.CurrentState = UIState.MapSelection;
    }


    public static void SetStream(ReplayStreamingSocket stream)
    {
        if (Stream != null)
        {
            Reset();
        }

        Stream = stream;
        IsStreaming = true;
        UIStateManager.OnUIStateChanged += UpdateUIState;
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateDifficulty;
    }


    public static void Reset()
    {
        Stream?.Dispose();
        Stream = null;
        IsStreaming = false;

        UIStateManager.OnUIStateChanged -= UpdateUIState;
        BeatmapManager.OnBeatmapDifficultyChanged -= UpdateDifficulty;
    }
}