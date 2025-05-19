using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class BeatmapManager : MonoBehaviour
{
    private static List<Difficulty> Difficulties = new List<Difficulty>();

    private static BeatmapInfo _info = new BeatmapInfo();
    public static BeatmapInfo Info
    {
        get => _info;

        set
        {
            _info = value;

            if(_info.audio.bpm <= 0)
            {
                //Shrimply to avoid crashes
                _info.audio.bpm = 0.0001f;
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
            if(Mathf.Approximately(NJS, 0f))
            {
                switch(_currentDifficulty.difficultyRank)
                {
                    default:
                    case DifficultyRank.Easy:
                    case DifficultyRank.Normal:
                    case DifficultyRank.Hard:
                        NJS = 10f;
                        break;
                    case DifficultyRank.Expert:
                        NJS = 12f;
                        break;
                    case DifficultyRank.ExpertPlus:
                        NJS = 16f;
                        break;
                }
            }

            DefaultHJD = 4;
            while(GetJumpDistance(DefaultHJD, Info.audio.bpm, NJS) > 35.998f)
            {
                DefaultHJD /= 2;
            }

            if(ReplayManager.IsReplayMode)
            {
                DefaultJumpDistance = ReplayManager.CurrentReplay.info.jumpDistance;
            }
            else DefaultJumpDistance = GetJumpDistance(HJD, Info.audio.bpm, NJS);
            JumpDistance = DefaultJumpDistance;

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

    public static string EnvironmentName => CurrentDifficulty.environmentName;

    public static bool MappingExtensions { get; private set; }
    public static bool NoodleExtensions { get; private set; }

    public static float DefaultHJD { get; private set; }
    public static float DefaultJumpDistance { get; private set; }

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


    public static float GetJumpDistance(float HJD, float BPM, float NJS)
    {
        float RT = 60f / BPM * HJD;
        return NJS * 2 * RT;
    }


    public static float GetCustomRT(float customOffset)
    {
        float hjd = DefaultHJD + customOffset;
        float bpm = Info.audio.bpm;
        return 60f / bpm * hjd;
    }


    public static List<Difficulty> GetDifficultiesByCharacteristic(DifficultyCharacteristic characteristic)
    {
        return Difficulties.FindAll(x => x.characteristic == characteristic);
    }


    public static bool HasCharacteristic(DifficultyCharacteristic characteristic)
    {
        return Difficulties.Any(x => x.characteristic == characteristic);
    }


    private static Difficulty GetDefaultDifficultyFromCharacteristic(List<Difficulty> difficulties)
    {
        Difficulty max = null;
        foreach(Difficulty difficulty in difficulties)
        {
            if(max == null || difficulty.difficultyRank > max.difficultyRank)
            {
                max = difficulty;
            }
        }
        return max;
    }


    public static Difficulty GetDefaultDifficulty()
    {
        if(Difficulties.Count == 0)
        {
            return Difficulty.Empty;
        }

        IEnumerable<DifficultyCharacteristic> characteristics = Enum.GetValues(typeof(DifficultyCharacteristic)).Cast<DifficultyCharacteristic>();
        foreach(DifficultyCharacteristic characteristic in characteristics)
        {
            List<Difficulty> difficulties = GetDifficultiesByCharacteristic(characteristic);
            if(difficulties.Count > 0)
            {
                return GetDefaultDifficultyFromCharacteristic(difficulties);
            }
        }

        return Difficulty.Empty;
    }


    public static void SetDifficulties(List<Difficulty> difficulties)
    {
        Difficulties.Clear();
        Difficulties.AddRange(difficulties.OrderBy(x => x.difficultyRank));
    }


    public void UpdateUIState(UIState newState)
    {
        if(newState != UIState.Previewer)
        {
            Difficulties.Clear();
            Info = new BeatmapInfo();
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

    public string[] mappers;
    public string[] lighters;

    public string label;
    public string[] requirements;
    public NullableColorPalette songCoreColors;


    public static Difficulty Empty => new Difficulty
    {
        characteristic = DifficultyCharacteristic.Unknown,
        difficultyRank = DifficultyRank.ExpertPlus,
        beatmapDifficulty = BeatmapDifficulty.GetDefault(),
        noteJumpSpeed = 0,
        spawnOffset = 0,
        environmentName = "DefaultEnvironment",
        colorScheme = null,
        mappers = new string[0],
        lighters = new string[0],
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