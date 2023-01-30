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
        get
        {
            return _info;
        }
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
        get
        {
            return _currentMap;
        }
        set
        {
            _currentMap = value;

            if(_currentMap.NoteJumpSpeed == 0)
            {
                //Game defaults to 16 on expert+ when njs is 0
                _currentMap.NoteJumpSpeed = 12 + (int)_currentMap.difficultyRank;
            }

            Debug.Log($"Current diff is {value.characteristic}, {value.difficultyRank}");
            OnBeatmapDifficultyChanged?.Invoke(value);
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

    public static float HJD
    {
        get
        {
            return DefaultHJD + CurrentMap.SpawnOffset;
        }
    }
    public static float ReactionTime
    {
        get
        {
            return (60 / Info._beatsPerMinute) * HJD;
        }
    }
    public static float JumpDistance
    {
        get
        {
            return GetJumpDistance(HJD, Info._beatsPerMinute, CurrentMap.NoteJumpSpeed);
        }
    }


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


    public static void SetDefaultDifficulty()
    {
        //Load the default difficulty
        //Else if chains my behated (I can't think of a better way to do this)
        if(Difficulties.Count == 0)
        {
            Debug.LogWarning("Map has no difficulties!");
        }
        else if(StandardDifficulties.Count > 0)
        {
            CurrentMap = StandardDifficulties[StandardDifficulties.Count - 1];
        }
        else if(OneSaberDifficulties.Count > 0)
        {
            CurrentMap = OneSaberDifficulties[OneSaberDifficulties.Count - 1];
        }
        else if(NoArrowsDifficulties.Count > 0)
        {
            CurrentMap = NoArrowsDifficulties[NoArrowsDifficulties.Count - 1];
        }
        else if(ThreeSixtyDifficulties.Count > 0)
        {
            CurrentMap = ThreeSixtyDifficulties[ThreeSixtyDifficulties.Count - 1];
        }
        else if(NinetyDifficulties.Count > 0)
        {
            CurrentMap = NinetyDifficulties[NinetyDifficulties.Count - 1];
        }
        else if(LightshowDifficulties.Count > 0)
        {
            CurrentMap = LightshowDifficulties[LightshowDifficulties.Count - 1];
        }
        else if(LawlessDifficulties.Count > 0)
        {
            CurrentMap = LawlessDifficulties[LawlessDifficulties.Count - 1];
        }
        else if(UnknownDifficulties.Count > 0)
        {
            CurrentMap = UnknownDifficulties[UnknownDifficulties.Count - 1];
        }
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


    public static Difficulty Empty = new Difficulty
    {
        characteristic = DifficultyCharacteristic.Unknown,
        difficultyRank = DifficultyRank.ExpertPlus,
        beatmapDifficulty = BeatmapDifficulty.Empty,
        NoteJumpSpeed = 0,
        SpawnOffset = 0,
        Label = "Expert+"
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