using System;
using UnityEngine;

[Serializable]
public class BeatmapInfo
{
    //Version doesn't need to be internally stored.
    public string _songName;
    public string _songSubName;
    public string _songAuthorName;
    public string _levelAuthorName;
    public float _beatsPerMinute;
    //Shuffle is depreciated and will be ignored.
    //Preview times aren't needed either
    public string _songFilename;
    public string _coverImageFilename;
    public string _environmentName;
    public string _allDirectionsEnvironmentName;
    public float _songTimeOffset;
    public DifficultyBeatmapSet[] _difficultyBeatmapSets;

    public string[] _environmentNames;
    public ColorSchemeContainer[] _colorSchemes;


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


    public static string TrimCharacteristicString(string characteristicName)
    {
        //Trims extra text added by BeatLeader in modded modes
        characteristicName = characteristicName.TrimEnd("OldDots");
        characteristicName = characteristicName.TrimEnd("-PinkPlay_Controllable");

        characteristicName = characteristicName.TrimStart("Inverse");
        characteristicName = characteristicName.TrimStart("Inverted");
        characteristicName = characteristicName.TrimStart("Vertical");
        characteristicName = characteristicName.TrimStart("Horizontal");

        return characteristicName;
    }


    public static DifficultyCharacteristic CharacteristicFromString(string characteristicName)
    {
        characteristicName = TrimCharacteristicString(characteristicName);

        if(characteristicName.Equals("360Degree", StringComparison.InvariantCultureIgnoreCase))
        {
            return DifficultyCharacteristic.ThreeSixty;
        }
        if(characteristicName.Equals("90Degree", StringComparison.InvariantCultureIgnoreCase))
        {
            return DifficultyCharacteristic.Ninety;
        }

        DifficultyCharacteristic characteristic;
        bool success = Enum.TryParse(characteristicName, true, out characteristic);
        if(!success)
        {
            Debug.LogWarning($"Could not match characteristic name: {characteristicName}");
            return DifficultyCharacteristic.Unknown;
        }
        else return characteristic;
    }


    public static DifficultyRank DifficultyRankFromString(string difficultyName)
    {
        DifficultyRank difficulty;
        bool success = Enum.TryParse(difficultyName, true, out difficulty);
        if(!success)
        {
            Debug.LogWarning("Could not match difficulty name!");
            return DifficultyRank.ExpertPlus;
        }
        else return difficulty;
    }


    public static readonly BeatmapInfo Empty = new BeatmapInfo
    {
        _songName = "",
        _songSubName = "",
        _songAuthorName = "",
        _levelAuthorName = "",
        _beatsPerMinute = 120,
        _songFilename = "",
        _coverImageFilename = "",
        _environmentName = "",
        _allDirectionsEnvironmentName = "",
        _songTimeOffset = 0,
        _difficultyBeatmapSets = new DifficultyBeatmapSet[0],
        _environmentNames = new string[0],
        _colorSchemes = new ColorSchemeContainer[0]
    };
}


[Serializable]
public struct DifficultyBeatmapSet
{
    public string _beatmapCharacteristicName;
    public DifficultyBeatmap[] _difficultyBeatmaps;
}


[Serializable]
public struct DifficultyBeatmap
{
    public string _difficulty;
    public int _difficultyRank;
    public string _beatmapFilename;
    public float _noteJumpMovementSpeed;
    public float _noteJumpStartBeatOffset;

    public int _beatmapColorSchemeIdx;
    public int _environmentNameIdx;

    public CustomDifficultyData _customData;
}


[Serializable]
public class ColorSchemeContainer
{
    public bool useOverride;
    public ColorScheme colorScheme;
}


[Serializable]
public class ColorScheme
{
    public string colorSchemeId;
    public SerializableColor saberAColor;
    public SerializableColor saberBColor;
    public SerializableColor environmentColor0;
    public SerializableColor environmentColor1;
    public SerializableColor obstaclesColor;
    public SerializableColor environmentColor0Boost;
    public SerializableColor environmentColor1Boost;


    public NullableColorPalette GetPalette()
    {
        return new NullableColorPalette
        {
            LeftNoteColor = saberAColor?.GetColor(),
            RightNoteColor = saberBColor?.GetColor(),
            LightColor1 = environmentColor0?.GetColor(),
            LightColor2 = environmentColor1?.GetColor(),
            WhiteLightColor = Color.white,
            BoostLightColor1 = environmentColor0Boost?.GetColor(),
            BoostLightColor2 = environmentColor1Boost?.GetColor(),
            BoostWhiteLightColor = Color.white,
            WallColor = obstaclesColor?.GetColor(),
        };
    }
}


[Serializable]
public class CustomDifficultyData
{
    public string _difficultyLabel;
    public string[] _requirements;

    //SongCore color overrides
    public SerializableColor _colorLeft;
    public SerializableColor _colorRight;
    public SerializableColor _envColorLeft;
    public SerializableColor _envColorRight;
    public SerializableColor _envColorWhite;
    public SerializableColor _envColorLeftBoost;
    public SerializableColor _envColorRightBoost;
    public SerializableColor _envColorWhiteBoost;
    public SerializableColor _obstacleColor;
}


[Serializable]
public class SerializableColor
{
    public float r;
    public float g;
    public float b;
    public float? a;


    public Color GetColor()
    {
        return new Color(r, g, b, a ?? 1f);
    }
}


public enum DifficultyRank
{
    Easy,
    Normal,
    Hard,
    Expert,
    ExpertPlus
}


public enum DifficultyCharacteristic
{
    Standard,
    OneSaber,
    NoArrows,
    ThreeSixty,
    Ninety,
    Legacy,
    Lightshow,
    Lawless,
    Unknown
}