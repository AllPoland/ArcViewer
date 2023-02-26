using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class BeatmapManager : MonoBehaviour
{
    public static List<Difficulty> Difficulties = new List<Difficulty>();

    public static List<Difficulty> StandardDifficulties => GetDifficultiesByCharacteristic(DifficultyCharacteristic.Standard);

    public static List<Difficulty> OneSaberDifficulties => GetDifficultiesByCharacteristic(DifficultyCharacteristic.OneSaber);

    public static List<Difficulty> NoArrowsDifficulties => GetDifficultiesByCharacteristic(DifficultyCharacteristic.NoArrows);

    public static List<Difficulty> ThreeSixtyDifficulties => GetDifficultiesByCharacteristic(DifficultyCharacteristic.ThreeSixty);

    public static List<Difficulty> NinetyDifficulties => GetDifficultiesByCharacteristic(DifficultyCharacteristic.Ninety);

    public static List<Difficulty> LightshowDifficulties => GetDifficultiesByCharacteristic(DifficultyCharacteristic.Lightshow);

    public static List<Difficulty> LawlessDifficulties => GetDifficultiesByCharacteristic(DifficultyCharacteristic.Lawless);

    public static List<Difficulty> UnknownDifficulties => GetDifficultiesByCharacteristic(DifficultyCharacteristic.Unknown);


    private static BeatmapInfo _info = BeatmapInfo.Empty;
    public static BeatmapInfo Info
    {
        get => _info;

        set
        {
            _info = value;

            if(_info._beatsPerMinute == 0)
            {
                //Shrimply to avoid crashes
                _info._beatsPerMinute = 0.0001f;
            }

            OnBeatmapInfoChanged?.Invoke(value);
        }
    }

    private static Difficulty _currentMap = Difficulty.Empty;
    public static Difficulty CurrentMap
    {
        get => _currentMap;
        
        set
        {
            _currentMap = value;

            if(_currentMap.NoteJumpSpeed == 0)
            {
                switch(_currentMap.difficultyRank)
                {
                    case DifficultyRank.Easy:
                    case DifficultyRank.Normal:
                    case DifficultyRank.Hard:
                        _currentMap.NoteJumpSpeed = 10;
                        break;
                    case DifficultyRank.Expert:
                        _currentMap.NoteJumpSpeed = 12;
                        break;
                    case DifficultyRank.ExpertPlus:
                        _currentMap.NoteJumpSpeed = 16;
                        break;
                }
            }

            if(_currentMap.requirements.Contains("Noodle Extensions"))
            {
                ErrorHandler.Instance.DisplayPopup(ErrorType.Warning, "Noodle Extensions is not yet supported! Things may not display correctly.");
            }
            else if(_currentMap.requirements.Contains("Mapping Extensions"))
            {
                ErrorHandler.Instance.DisplayPopup(ErrorType.Warning, "Mapping Extensions is not yet supported! Things may not display correctly.");
            }

            Debug.Log($"Current diff is {_currentMap.characteristic}, {_currentMap.difficultyRank}");
            OnBeatmapDifficultyChanged?.Invoke(_currentMap);
        }
    }

    public static float DefaultHJD
    {
        get
        {
            float value = 4;

            float JD = GetJumpDistance(value, Info._beatsPerMinute, CurrentMap.NoteJumpSpeed);
            while(JD > 35.998f && value > 0.25f)
            {
                value /= 2;
                JD = GetJumpDistance(value, Info._beatsPerMinute, CurrentMap.NoteJumpSpeed);
            }

            if(value < 0.25f)
            {
                value = 0.25f;
            }
            
            return value;
        }
    }

    public static float HJD => DefaultHJD + CurrentMap.SpawnOffset;
    public static float ReactionTime => (60 / Info._beatsPerMinute) * HJD;
    public static float JumpDistance => GetJumpDistance(HJD, Info._beatsPerMinute, CurrentMap.NoteJumpSpeed);


    public static float GetJumpDistance(float HJD, float BPM, float NJS)
    {
        float RT = (60 / BPM) * HJD;
        return NJS * 2 * RT;
    }


    public static List<Difficulty> GetDifficultiesByCharacteristic(DifficultyCharacteristic characteristic)
    {
        return Difficulties.FindAll(x => x.characteristic == characteristic);
    }


    public static bool HasCharacteristic(DifficultyCharacteristic characteristic)
    {
        return Difficulties.Any(x => x.characteristic == characteristic);
    }


    public static Difficulty GetDefaultDifficulty()
    {
        //Else if chains my behated (I can't think of a better way to do this)
        if(Difficulties.Count == 0)
        {
            Debug.LogWarning("Map has no difficulties!");
            return Difficulty.Empty;
        }
        else if(StandardDifficulties.Count > 0)
        {
            return StandardDifficulties.Last();
        }
        else if(OneSaberDifficulties.Count > 0)
        {
            return OneSaberDifficulties.Last();
        }
        else if(NoArrowsDifficulties.Count > 0)
        {
            return NoArrowsDifficulties.Last();
        }
        else if(ThreeSixtyDifficulties.Count > 0)
        {
            return ThreeSixtyDifficulties.Last();
        }
        else if(NinetyDifficulties.Count > 0)
        {
            return NinetyDifficulties[NinetyDifficulties.Count - 1];
        }
        else if(LightshowDifficulties.Count > 0)
        {
            return LightshowDifficulties.Last();
        }
        else if(LawlessDifficulties.Count > 0)
        {
            return LawlessDifficulties.Last();
        }
        else if(UnknownDifficulties.Count > 0)
        {
            return UnknownDifficulties.Last();
        }

        return Difficulty.Empty;
    }


    public void UpdateUIState(UIState newState)
    {
        if(newState != UIState.Previewer)
        {
            Difficulties.Clear();
            CurrentMap = Difficulty.Empty;
            Info = new BeatmapInfo();
        }
    }


    public static event Action<BeatmapInfo> OnBeatmapInfoChanged;
    public static event Action<Difficulty> OnBeatmapDifficultyChanged;


    private void OnEnable()
    {
        UIStateManager.OnUIStateChanged += UpdateUIState;
    }
}


public class Difficulty
{
    public DifficultyCharacteristic characteristic;
    public DifficultyRank difficultyRank;
    public BeatmapDifficulty beatmapDifficulty;
    public float NoteJumpSpeed;
    public float SpawnOffset;
    public string Label;
    public string[] requirements;


    public static Difficulty Empty = new Difficulty
    {
        characteristic = DifficultyCharacteristic.Unknown,
        difficultyRank = DifficultyRank.ExpertPlus,
        beatmapDifficulty = BeatmapDifficulty.Empty,
        NoteJumpSpeed = 0,
        SpawnOffset = 0,
        Label = "Expert+",
        requirements = new string[0]
    };


    public static string DiffLabelFromRank(DifficultyRank rank)
    {
        if(rank == DifficultyRank.ExpertPlus)
        {
            return "Expert+";
        }
        else return rank.ToString();
    }
}