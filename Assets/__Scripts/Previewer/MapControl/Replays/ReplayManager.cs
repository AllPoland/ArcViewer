using System;
using System.Linq;
using UnityEngine;

public class ReplayManager : MonoBehaviour
{
    public const string BeatLeaderURL = "https://www.beatleader.xyz/";

    public static bool IsReplayMode { get; private set; }
    public static Replay CurrentReplay { get; private set; }

    public static Sprite Avatar { get; private set; }
    public static BeatleaderUser PlayerInfo;
    public static string LeaderboardID = "";

    public static event Action<bool> OnReplayModeChanged;
    public static event Action<Replay> OnReplayUpdated;

    public static event Action<Sprite> OnAvatarUpdated;

    public static float PlayerHeight;
    public static string[] Modifiers = new string[0];

    //This will need to be updated for the new songcore features but it'll do for now
    public static bool OneSaber => IsReplayMode && BeatmapManager.CurrentDifficulty.characteristic == DifficultyCharacteristic.OneSaber;
    public static bool LeftHandedMode => IsReplayMode && CurrentReplay.info.leftHanded;

    public static float ReplayTimeScale { get; private set; }
    public static bool BatteryEnergy { get; private set; }
    public static bool OneLife { get; private set; }
    public static bool NoArrows { get; private set; }
    public static bool NoWalls { get; private set; }
    public static bool NoBombs { get; private set; }

    public static bool Failed = false;
    public static float FailTime = 0f;
    public static bool HasFailed => Failed && TimeManager.CurrentTime >= FailTime;

    private static MapElementList<PlayerHeightEvent> playerHeightEvents = new MapElementList<PlayerHeightEvent>();


    private static bool HasModifier(string modifier)
    {
        return Modifiers.Any(x => x.Equals(modifier, StringComparison.InvariantCultureIgnoreCase));
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

        Modifiers = CurrentReplay.info.modifiers.Split(',');
        BatteryEnergy = HasModifier("BE");

        NoArrows = HasModifier("NA");
        NoWalls = HasModifier("NO");
        NoBombs = HasModifier("NB");

        if(HasModifier("SF"))
        {
            //Super fast song
            ReplayTimeScale = 1.5f;
        }
        else if(HasModifier("FS"))
        {
            //Faster song
            ReplayTimeScale = 1.2f;
        }
        else if(HasModifier("SS"))
        {
            //Slower song
            ReplayTimeScale = 0.85f;
        }
        else ReplayTimeScale = 1f;

        OnReplayModeChanged?.Invoke(true);
        OnReplayUpdated?.Invoke(CurrentReplay);

        TimeManager.OnBeatChangedEarly += UpdateBeat;

        ReplayInfo info = CurrentReplay.info;
        Debug.Log($"Loaded replay for {info.songName}, {info.mode}, {info.difficulty}, played by {info.playerName}, with score {info.score}, and modifiers: {info.modifiers}");
    }


    public static void SetAvatarImageData(byte[] imageData)
    {
        ClearAvatar();

        Texture2D newTexture = new Texture2D(2, 2);
        bool loaded = newTexture.LoadImage(imageData);

        if(!loaded)
        {
            Debug.LogWarning("Unable to load player avatar!");
            ErrorHandler.Instance?.ShowPopup(ErrorType.Warning, "Unable to load avatar image!");

            Destroy(newTexture);
            Avatar = null;
        }
        else
        {
            Avatar = Sprite.Create(newTexture,
                new Rect(0, 0, newTexture.width, newTexture.height),
                new Vector2(0.5f, 0.5f), 100);
        }
        OnAvatarUpdated?.Invoke(Avatar);
    }


    private static void ClearAvatar()
    {
        if(Avatar == null)
        {
            return;
        }

        OnAvatarUpdated?.Invoke(null);
        Destroy(Avatar.texture);
        Destroy(Avatar);
        Avatar = null;
    }


    private static void UpdatePlayerHeight(float beat)
    {
        int lastHeightIndex = playerHeightEvents.GetLastIndex(TimeManager.CurrentTime, x => x.Time <= TimeManager.CurrentTime);
        PlayerHeight = lastHeightIndex >= 0
            ? playerHeightEvents[lastHeightIndex].Height
            : CurrentReplay.info.height;
        
        if(PlayerHeight <= 0.001)
        {
            //Some old replays didn't store player height, so use default
            PlayerHeight = ObjectManager.DefaultPlayerHeight;
        }
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

        ClearAvatar();
        PlayerInfo = null;
        LeaderboardID = "";

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