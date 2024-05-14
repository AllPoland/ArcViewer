using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class BeatmapInfoV2
{
    public string _version;
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
    public DifficultyBeatmapSetV2[] _difficultyBeatmapSets;

    public string[] _environmentNames;
    public ColorSchemeContainerV2[] _colorSchemes;

    public bool HasFields => !string.IsNullOrEmpty(_version) || !string.IsNullOrEmpty(_songName)
        || !string.IsNullOrEmpty(_songSubName) || !string.IsNullOrEmpty(_songAuthorName)
        || !string.IsNullOrEmpty(_levelAuthorName) || !string.IsNullOrEmpty(_songFilename)
        || !string.IsNullOrEmpty(_coverImageFilename) || !string.IsNullOrEmpty(_environmentName)
        || !string.IsNullOrEmpty(_allDirectionsEnvironmentName) || _difficultyBeatmapSets != null
        || _environmentNames != null || _colorSchemes != null;


    public BeatmapInfoV2()
    {
        _version = "2.1.0";
        _songName = "";
        _songSubName = "";
        _songAuthorName = "";
        _levelAuthorName = "";
        _beatsPerMinute = 120;
        _songFilename = "";
        _coverImageFilename = "";
        _environmentName = "DefaultEnvironment";
        _allDirectionsEnvironmentName = "GlassDesertEnvironment";
        _songTimeOffset = 0;
        _difficultyBeatmapSets = new DifficultyBeatmapSetV2[0];
        _environmentNames = new string[0];
        _colorSchemes = new ColorSchemeContainerV2[0];
    }


    public BeatmapInfo ConvertToV4()
    {
        BeatmapInfo info = new BeatmapInfo();

        info.song = new BeatmapInfoSong
        {
            title = _songName,
            subTitle = _songSubName,
            author = _songAuthorName
        };

        info.audio = new BeatmapInfoAudio
        {
            songFilename = _songFilename,
            bpm = _beatsPerMinute
        };

        info.coverImageFilename = _coverImageFilename;

        List<string> environmentNames = _environmentNames != null ? _environmentNames.ToList() : new List<string>();

        //Make sure the new environment names includes the default environments
        if(!environmentNames.Contains(_environmentName))
        {
            environmentNames.Add(_environmentName);
        }
        if(!environmentNames.Contains(_allDirectionsEnvironmentName))
        {
            environmentNames.Add(_allDirectionsEnvironmentName);
        }

        info.environmentNames = environmentNames.ToArray();

        //Convert color schemes
        List<BeatmapInfoColorScheme> colorSchemes = new List<BeatmapInfoColorScheme>();
        foreach(ColorSchemeContainerV2 container in _colorSchemes)
        {
            if(!container.useOverride)
            {
                //This override shouldn't be used
                //Why the fuck was this a thing?
                continue;
            }

            BeatmapInfoColorSchemeV2 scheme = container.colorScheme;
            colorSchemes.Add(new BeatmapInfoColorScheme(scheme.colorSchemeId, scheme.GetPalette()));
        }
        info.colorSchemes = colorSchemes.ToArray();

        //Convert difficulties
        List<DifficultyBeatmap> difficultyBeatmaps = new List<DifficultyBeatmap>();

        //Every difficulty will have the same authors
        string[] authors = {_levelAuthorName};
        BeatmapAuthors beatmapAuthors = new BeatmapAuthors
        {
            mappers = authors,
            lighters = new string[0]
        };

        foreach(DifficultyBeatmapSetV2 set in _difficultyBeatmapSets)
        {
            string characteristicName = set._beatmapCharacteristicName;
            foreach(DifficultyBeatmapV2 beatmapV2 in set._difficultyBeatmaps)
            {
                DifficultyBeatmap beatmap = beatmapV2.ConvertToV4(characteristicName);
                beatmap.beatmapAuthors = beatmapAuthors;

                //Environment indexes need to be adjusted since the arrays don't
                //always persist through the conversion
                string environmentName;
                if(beatmapV2._environmentNameIdx > 0 && beatmapV2._environmentNameIdx < _environmentNames.Length)
                {
                    //The environment name is listed in this v2 info
                    environmentName = _environmentNames[beatmapV2._environmentNameIdx];
                }
                else
                {
                    //The environment name isn't listed, use the info's default
                    DifficultyCharacteristic characteristic = BeatmapInfo.CharacteristicFromString(characteristicName);
                    bool isAllDirections = characteristic == DifficultyCharacteristic.ThreeSixty || characteristic == DifficultyCharacteristic.Ninety;

                    environmentName = isAllDirections ? _allDirectionsEnvironmentName : _environmentName;
                }

                //We can be certain that the new environment names list includes this name
                beatmap.environmentNameIdx = environmentNames.IndexOf(environmentName);

                beatmap.customData = beatmapV2._customData?.ConvertToV4();

                difficultyBeatmaps.Add(beatmap);
            }
        }
        info.difficultyBeatmaps = difficultyBeatmaps.ToArray();

        //Include song time offset if needed
        if(!_songTimeOffset.Approximately(0f))
        {
            info.songTimeOffset = _songTimeOffset;
        }

        return info;
    }
}


[Serializable]
public struct DifficultyBeatmapSetV2
{
    public string _beatmapCharacteristicName;
    public DifficultyBeatmapV2[] _difficultyBeatmaps;
}


[Serializable]
public struct DifficultyBeatmapV2
{
    public string _difficulty;
    public int _difficultyRank;
    public string _beatmapFilename;
    public float _noteJumpMovementSpeed;
    public float _noteJumpStartBeatOffset;

    public int _beatmapColorSchemeIdx;
    public int _environmentNameIdx;

    public DifficultyBeatmapCustomDataV2 _customData;


    public DifficultyBeatmap ConvertToV4(string characteristic)
    {
        return new DifficultyBeatmap
        {
            characteristic = characteristic,
            difficulty = _difficulty,
            beatmapColorSchemeIdx = _beatmapColorSchemeIdx,
            noteJumpMovementSpeed = _noteJumpMovementSpeed,
            noteJumpStartBeatOffset = _noteJumpStartBeatOffset,
            beatmapDataFilename = _beatmapFilename,
            lightshowDataFilename = ""
        };
    }
}


[Serializable]
public class ColorSchemeContainerV2
{
    public bool useOverride;
    public BeatmapInfoColorSchemeV2 colorScheme;
}


[Serializable]
public class BeatmapInfoColorSchemeV2
{
    public string colorSchemeId;
    public BeatmapInfoColorV2 saberAColor;
    public BeatmapInfoColorV2 saberBColor;
    public BeatmapInfoColorV2 environmentColor0;
    public BeatmapInfoColorV2 environmentColor1;
    public BeatmapInfoColorV2 obstaclesColor;
    public BeatmapInfoColorV2 environmentColor0Boost;
    public BeatmapInfoColorV2 environmentColor1Boost;


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
public class DifficultyBeatmapCustomDataV2
{
    public string _difficultyLabel;
    public string[] _requirements;

    //SongCore color overrides
    public BeatmapInfoColorV2 _colorLeft;
    public BeatmapInfoColorV2 _colorRight;
    public BeatmapInfoColorV2 _envColorLeft;
    public BeatmapInfoColorV2 _envColorRight;
    public BeatmapInfoColorV2 _envColorWhite;
    public BeatmapInfoColorV2 _envColorLeftBoost;
    public BeatmapInfoColorV2 _envColorRightBoost;
    public BeatmapInfoColorV2 _envColorWhiteBoost;
    public BeatmapInfoColorV2 _obstacleColor;


    public DifficultyBeatmapCustomData ConvertToV4()
    {
        return new DifficultyBeatmapCustomData
        {
            difficultyLabel = _difficultyLabel,
            requirements = _requirements,
            colorLeft = _colorLeft,
            colorRight = _colorRight,
            envColorLeft = _envColorLeft,
            envColorRight = _envColorRight,
            envColorWhite = _envColorWhite,
            envColorLeftBoost = _envColorLeftBoost,
            envColorRightBoost = _envColorRightBoost,
            envColorWhiteBoost = _envColorWhiteBoost,
            obstacleColor = _obstacleColor
        };
    }
}


[Serializable]
public class BeatmapInfoColorV2
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