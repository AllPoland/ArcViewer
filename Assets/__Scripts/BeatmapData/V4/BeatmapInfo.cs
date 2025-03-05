using System;
using UnityEngine;

[Serializable]
public class BeatmapInfo
{
    public string version;

    public BeatmapInfoSong song;
    public BeatmapInfoAudio audio;

    public string songPreviewFilename;
    public string coverImageFilename;
    public string[] environmentNames;

    public BeatmapInfoColorScheme[] colorSchemes;
    public DifficultyBeatmap[] difficultyBeatmaps;

    //This is depreciated, but still needed for converted V2 maps
    [NonSerialized] public float? songTimeOffset;

    public bool HasFields => !string.IsNullOrEmpty(version) || song != null
        || audio != null || !string.IsNullOrEmpty(songPreviewFilename)
        || !string.IsNullOrEmpty(coverImageFilename) || environmentNames != null
        || colorSchemes != null || difficultyBeatmaps != null;


    public BeatmapInfo()
    {
        version = "4.0.0";
        song = new BeatmapInfoSong();
        audio = new BeatmapInfoAudio();

        songPreviewFilename = "";
        coverImageFilename = "";
        environmentNames = new string[1] {"DefaultEnvironment"};

        colorSchemes = new BeatmapInfoColorScheme[0];
        difficultyBeatmaps = new DifficultyBeatmap[0];

        songTimeOffset = null;
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
}


[Serializable]
public class BeatmapInfoSong
{
    public string title;
    public string subTitle;
    public string author;


    public BeatmapInfoSong()
    {
        title = "";
        subTitle = "";
        author = "";
    }
}


[Serializable]
public class BeatmapInfoAudio
{
    public string songFilename;
    public float songDuration;
    public string audioDataFilename;

    public float bpm;
    public float lufs;

    public float previewStartTime;
    public float previewDuration;


    public BeatmapInfoAudio()
    {
        songFilename = "";
        songDuration = 0f;
        audioDataFilename = "";
        bpm = 120f;
        lufs = 0f;
        previewStartTime = 0f;
        previewDuration = 0f;
    }
}


[Serializable]
public class BeatmapInfoColorScheme
{
    public bool useOverride;
    public bool overrideNotes;
    public bool overrideLights;

    public string colorSchemeName;

    public string saberAColor;
    public string saberBColor;

    public string obstaclesColor;

    public string environmentColor0;
    public string environmentColor1;
    public string environmentColor0Boost;
    public string environmentColor1Boost;


    public BeatmapInfoColorScheme(string name, NullableColorPalette palette)
    {
        useOverride = true;
        overrideNotes = true;
        overrideLights = true;

        colorSchemeName = name;

        saberAColor = HexFromColor(palette?.LeftNoteColor);
        saberBColor = HexFromColor(palette?.RightNoteColor);

        obstaclesColor = HexFromColor(palette?.WallColor);

        environmentColor0 = HexFromColor(palette?.LightColor1);
        environmentColor1 = HexFromColor(palette?.LightColor2);
        environmentColor0Boost = HexFromColor(palette?.BoostLightColor1);
        environmentColor1Boost = HexFromColor(palette?.BoostLightColor2);
    }


    public NullableColorPalette GetPalette()
    {
        if(!useOverride && !overrideNotes && !overrideLights)
        {
            return null;
        }

        return new NullableColorPalette
        {
            LeftNoteColor = overrideNotes ? ColorFromHex(saberAColor) : null,
            RightNoteColor = overrideNotes ? ColorFromHex(saberBColor) : null,
            WallColor = overrideNotes ? ColorFromHex(obstaclesColor) : null,
            LightColor1 = overrideLights ? ColorFromHex(environmentColor0) : null,
            LightColor2 = overrideLights ? ColorFromHex(environmentColor1) : null,
            WhiteLightColor = overrideLights ? Color.white : null,
            BoostLightColor1 = overrideLights ? ColorFromHex(environmentColor1) : null,
            BoostLightColor2 = overrideLights ? ColorFromHex(environmentColor1Boost) : null,
            BoostWhiteLightColor = overrideLights ? Color.white : null
        };
    }


    public static string HexFromColor(Color? color)
    {
        return color != null ? ColorUtility.ToHtmlStringRGBA((Color)color) : null;
    }


    public static Color? ColorFromHex(string hex)
    {
        hex = hex.TrimStart('#');

        if(hex.Length < 6)
        {
            //Not enough characters to represent a hex value
            return null;
        }

        try
        {
            float r = Convert.ToByte(hex.Substring(0, 2), 16) / 255f;
            float g = Convert.ToByte(hex.Substring(2, 2), 16) / 255f;
            float b = Convert.ToByte(hex.Substring(4, 2), 16) / 255f;

            if(hex.Length < 8)
            {
                return new Color(r, g, b);
            }
            else
            {
                float a = Convert.ToByte(hex.Substring(6, 2), 16) / 255f;
                return new Color(r, g, b, a);
            }
        }
        catch
        {
            //Failed to convert a substring into a base 16 value
            return null;
        }
    }
}


[Serializable]
public class DifficultyBeatmap
{
    public string characteristic;
    public string difficulty;
    public BeatmapAuthors beatmapAuthors;

    public int environmentNameIdx;
    public int beatmapColorSchemeIdx;

    public float noteJumpMovementSpeed;
    public float noteJumpStartBeatOffset;

    public string lightshowDataFilename;
    public string beatmapDataFilename;

    public DifficultyBeatmapCustomData customData;


    public DifficultyBeatmap()
    {
        characteristic = "";
        difficulty = "";
        beatmapAuthors = new BeatmapAuthors();
        environmentNameIdx = 0;
        beatmapColorSchemeIdx = 0;
        noteJumpMovementSpeed = 10f;
        noteJumpStartBeatOffset = 0f;
        lightshowDataFilename = "";
        beatmapDataFilename = "";
        customData = null;
    }
}


[Serializable]
public class BeatmapAuthors
{
    public string[] mappers;
    public string[] lighters;


    public BeatmapAuthors()
    {
        mappers = new string[0];
        lighters = new string[0];
    }
}


public class DifficultyBeatmapCustomData
{
    //TODO: Figure out how songcore plans on handling custom data
    //This is all just a placeholder for now to carry over converted custom data
    public string difficultyLabel;
    public string[] requirements;

    //SongCore color overrides
    public BeatmapInfoColorV2 colorLeft;
    public BeatmapInfoColorV2 colorRight;
    public BeatmapInfoColorV2 envColorLeft;
    public BeatmapInfoColorV2 envColorRight;
    public BeatmapInfoColorV2 envColorWhite;
    public BeatmapInfoColorV2 envColorLeftBoost;
    public BeatmapInfoColorV2 envColorRightBoost;
    public BeatmapInfoColorV2 envColorWhiteBoost;
    public BeatmapInfoColorV2 obstacleColor;
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