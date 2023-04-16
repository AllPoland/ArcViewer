using System;
using UnityEngine;

[Serializable] public class BeatmapInfo
{
    //Version doesn't need to be internally stored.
    public string _songName;
    public string _songSubName;
    public string _songAuthorName;
    public string _levelAuthorName;
    public float _beatsPerMinute;
    //Shuffle is depreciated and will be ignored.
    public float _shuffle;
    public float _shufflePeriod;
    //Preview times aren't needed either
    public float _previewStartTime;
    public float _previewDuration;
    public string _songFilename;
    public string _coverImageFilename;
    public string _environmentName;
    public string _allDirectionsEnvironmentName;
    public float _songTimeOffset;
    public DifficultyBeatmapSet[] _difficultyBeatmapSets;


    public void AddNulls()
    {
        _songName = _songName ?? "Unknown";
        _songSubName = _songSubName ?? "";
        _songAuthorName = _songAuthorName ?? "Unknown";
        _levelAuthorName = _levelAuthorName ?? "Unknown";
        _songFilename = _songFilename ?? "";
        _coverImageFilename = _coverImageFilename ?? "";
        _environmentName = _environmentName ?? "DefaultEnvironment";
        _allDirectionsEnvironmentName = _allDirectionsEnvironmentName ?? "GlassDesertEnvironment";
        _difficultyBeatmapSets = _difficultyBeatmapSets ?? new DifficultyBeatmapSet[0];
    }


    public static DifficultyCharacteristic CharacteristicFromString(string characteristicName)
    {
        DifficultyCharacteristic characteristic;
        bool success = Enum.TryParse(characteristicName, true, out characteristic);
        if(!success)
        {
            Debug.LogWarning("Could not match characteristic name!");
            return DifficultyCharacteristic.Unknown;
        }
        else return characteristic;
    }


    public static readonly BeatmapInfo Empty = new BeatmapInfo
    {
        _songName = "",
        _songSubName = "",
        _songAuthorName = "",
        _levelAuthorName = "",
        _beatsPerMinute = 120,
        _shuffle = 0,
        _shufflePeriod = 0,
        _previewStartTime = 0,
        _previewDuration = 0,
        _songFilename = "",
        _coverImageFilename = "",
        _environmentName = "",
        _allDirectionsEnvironmentName = "",
        _songTimeOffset = 0,
        _difficultyBeatmapSets = new DifficultyBeatmapSet[0]
    };
}


[Serializable]
public class DifficultyBeatmap
{
    public string _difficulty;
    public int _difficultyRank;
    public string _beatmapFilename;
    public float _noteJumpMovementSpeed;
    public float _noteJumpStartBeatOffset;

    public CustomDifficultyData _customData;
}


[Serializable]
public class CustomDifficultyData
{
    public string _difficultyLabel;
    public string[] _requirements;
}


[Serializable]
public class DifficultyBeatmapSet
{
    public string _beatmapCharacteristicName;
    public DifficultyBeatmap[] _difficultyBeatmaps;
}


[Serializable]
public enum DifficultyRank
{
    Easy,
    Normal,
    Hard,
    Expert,
    ExpertPlus
}


[Serializable]
public enum DifficultyCharacteristic
{
    Standard,
    OneSaber,
    NoArrows,
    ThreeSixty,
    Ninety,
    Lightshow,
    Lawless,
    Unknown
}