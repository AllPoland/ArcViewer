using System;
using System.Threading.Tasks;
using UnityEngine;

public enum ReplaySourceType
{
    Unknown,
    BeatLeader,
    ScoreSaber
}

public class ReplaySourceInfo
{
    public ReplaySourceType SourceType;

    // player
    public string PlayerName;
    public string PlayerID;
    public string AvatarURL;
    public string PlayerProfileURL;

    // leaderboard
    public string LeaderboardURL;

    // player customization
    public Color? LeftSaberColor;
    public Color? RightSaberColor;

    // map resolution data for legacy replay patching
    public string MapHash;
    public int DifficultyRaw;
    public string Characteristic;
    public string MapID;

    // fallback map download info from the source API
    public string FallbackMapDownloadURL;
    public string FallbackMapID;

    // callback for loading additional data after replay is set
    public Func<Replay, Task> LoadSourceData;

    public string SourceName => SourceType switch
    {
        ReplaySourceType.BeatLeader => "BeatLeader",
        ReplaySourceType.ScoreSaber => "ScoreSaber",
        _ => ""
    };

    public bool HasPlayerProfile => !string.IsNullOrEmpty(PlayerProfileURL) && !string.IsNullOrEmpty(PlayerID);
    public bool HasLeaderboard => !string.IsNullOrEmpty(LeaderboardURL);
    public bool HasCustomColors => LeftSaberColor.HasValue && RightSaberColor.HasValue;
    public bool HasFallbackMap => !string.IsNullOrEmpty(FallbackMapDownloadURL);

    public void ApplyTo(Replay replay)
    {
        ReplayInfo info = replay.info;

        if(!string.IsNullOrEmpty(MapHash) && string.IsNullOrEmpty(info.hash))
            info.hash = MapHash;

        if(DifficultyRaw > 0 && string.IsNullOrEmpty(info.difficulty))
            info.difficulty = ReplayInfo.DifficultyNameFromRaw(DifficultyRaw);

        if(!string.IsNullOrEmpty(Characteristic) && string.IsNullOrEmpty(info.mode))
        {
            DifficultyCharacteristic parsed;
            bool validMode = Enum.TryParse<DifficultyCharacteristic>(Characteristic, true, out parsed)
                && parsed != DifficultyCharacteristic.Unknown;
            info.mode = validMode ? Characteristic : "Standard";
        }

        if(!string.IsNullOrEmpty(PlayerName))
            info.playerName = PlayerName;

        if(!string.IsNullOrEmpty(PlayerID) && string.IsNullOrEmpty(info.playerID))
            info.playerID = PlayerID;
    }
}


public class ResolvedScore
{
    public string ReplayURL;
    public string MapURL;
    public string MapID;
    public ReplaySourceInfo SourceInfo;
}
