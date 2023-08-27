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
    public static List<Difficulty> LegacyDifficulties => GetDifficultiesByCharacteristic(DifficultyCharacteristic.Legacy);
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

    private static Difficulty _currentDifficulty = Difficulty.Empty;
    public static Difficulty CurrentDifficulty
    {
        get => _currentDifficulty;
        
        set
        {
            _currentDifficulty = value;

            SpawnOffset = _currentDifficulty.spawnOffset;
            NJS = _currentDifficulty.noteJumpSpeed;
            if(NJS == 0)
            {
                switch(_currentDifficulty.difficultyRank)
                {
                    case DifficultyRank.Easy:
                    case DifficultyRank.Normal:
                    case DifficultyRank.Hard:
                        NJS = 10;
                        break;
                    case DifficultyRank.Expert:
                        NJS = 12;
                        break;
                    case DifficultyRank.ExpertPlus:
                        NJS = 16;
                        break;
                }
            }
            if(ReplayManager.IsReplayMode)
            {
                defaultJumpDistance = ReplayManager.CurrentReplay.info.jumpDistance;
            }
            else defaultJumpDistance = GetJumpDistance(HJD, Info._beatsPerMinute, NJS);
            JumpDistance = defaultJumpDistance;

            MappingExtensions = _currentDifficulty.requirements.Contains("Mapping Extensions");
            NoodleExtensions = _currentDifficulty.requirements.Contains("Noodle Extensions");

            if(NoodleExtensions)
            {
                ErrorHandler.Instance.ShowPopup(ErrorType.Warning, "Noodle Extensions is not fully supported! Things may not display correctly.");
            }
            else if(MappingExtensions)
            {
                ErrorHandler.Instance.ShowPopup(ErrorType.Warning, "Mapping Extensions support is incomplete! Things may not display correctly.");
            }

            Debug.Log($"Current diff is {_currentDifficulty.characteristic}, {_currentDifficulty.difficultyRank}");
            OnBeatmapDifficultyChanged?.Invoke(_currentDifficulty);
        }
    }

    public static string EnvironmentName => 
    (
        string.IsNullOrEmpty(CurrentDifficulty.environmentName)
            ? Info._environmentName
            : CurrentDifficulty.environmentName
    );

    public static bool MappingExtensions { get; private set; }
    public static bool NoodleExtensions { get; private set; }
    public static float defaultJumpDistance { get; private set; }

    public static float SpawnOffset;

    private static float _njs;
    public static float NJS
    {
        get => _njs;
        set
        {
            _njs = value;
            OnJumpSettingsChanged?.Invoke();
        }
    }

    private static float _jumpDistance;
    public static float JumpDistance
    {
        get => _jumpDistance;
        set
        {
            _jumpDistance = value;
            HalfJumpDistance = value / 2;
            OnJumpSettingsChanged?.Invoke();
        }
    }

    public static float HalfJumpDistance { get; private set; }

    public static float HJD => Mathf.Max(DefaultHJD + SpawnOffset, 0.25f);
    public static float ReactionTime => JumpDistance / 2 / NJS;

    private static float DefaultHJD
    {
        get
        {
            float value = 4;

            float JD = GetJumpDistance(value, Info._beatsPerMinute, NJS);
            while(JD > 35.998f)
            {
                value /= 2;
                JD = GetJumpDistance(value, Info._beatsPerMinute, NJS);
            }
            
            return value;
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


    public static Difficulty GetDefaultDifficulty()
    {
        //Else if chains my behated (I can't think of a better way to do this)
        if(Difficulties.Count == 0)
        {
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
            return NinetyDifficulties.Last();
        }
        else if(LegacyDifficulties.Count > 0)
        {
            return LegacyDifficulties.Last();
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
            Info = BeatmapInfo.Empty;
            CurrentDifficulty = Difficulty.Empty;
        }
    }


    public static event Action<BeatmapInfo> OnBeatmapInfoChanged;
    public static event Action<Difficulty> OnBeatmapDifficultyChanged;

    public static event Action OnJumpSettingsChanged;


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
    public float noteJumpSpeed;
    public float spawnOffset;

    public string environmentName;
    public NullableColorPalette colorScheme;

    public string label;
    public string[] requirements;
    public NullableColorPalette songCoreColors;


    public static Difficulty Empty => new Difficulty
    {
        characteristic = DifficultyCharacteristic.Unknown,
        difficultyRank = DifficultyRank.ExpertPlus,
        beatmapDifficulty = new BeatmapDifficulty(),
        noteJumpSpeed = 0,
        spawnOffset = 0,
        environmentName = "",
        colorScheme = null,
        label = "Expert+",
        requirements = new string[0],
        songCoreColors = null
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