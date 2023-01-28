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
    //Preview times aren't needed
    public float _previewStartTime;
    public float _previewDuration;
    public string _songFilename;
    public string _coverImageFilename;
    //Environments won't be used for a while
    public string _environmentName;
    public string _allDirectionsEnvironmentName;
    public float _songTimeOffset;
    //Custom data is unlikely to be pertinent. If it ends up being needed I might go through the pain of implementing it.
    public DifficultyBeatmapSet[] _difficultyBeatmapSets;


    public static DifficultyCharacteristic CharacteristicFromString(string characteristicName)
    {
        switch(characteristicName)
        {
            case "Standard": return DifficultyCharacteristic.Standard;
            case "OneSaber": return DifficultyCharacteristic.OneSaber;
            case "NoArrows": return DifficultyCharacteristic.NoArrows;
            case "360Degree": return DifficultyCharacteristic.ThreeSixty;
            case "90Degree": return DifficultyCharacteristic.Ninety;
            case "Lightshow": return DifficultyCharacteristic.Lightshow;
            case "Lawless": return DifficultyCharacteristic.Lawless;

            default:
                Debug.LogWarning("Could not match characteristic name!");
                return DifficultyCharacteristic.Unknown;
        }
    }
}


[Serializable] public class DifficultyBeatmap
{
    public string _difficulty;
    public int _difficultyRank;
    public string _beatmapFilename;
    public float _noteJumpMovementSpeed;
    public float _noteJumpStartBeatOffset;

    //Custom data will be added at a later date.
}


[Serializable] public class DifficultyBeatmapSet
{
    public string _beatmapCharacteristicName;
    public DifficultyBeatmap[] _difficultyBeatmaps;
}


[Serializable] public enum DifficultyRank
{
    Easy,
    Normal,
    Hard,
    Expert,
    ExpertPlus
}


[Serializable] public enum DifficultyCharacteristic
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