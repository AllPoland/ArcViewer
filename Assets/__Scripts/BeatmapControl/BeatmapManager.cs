using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatmapManager : MonoBehaviour
{
    public static BeatmapManager Instance { get; private set; }

    public List<Difficulty> Difficulties = new List<Difficulty>();

    public List<Difficulty> StandardDifficulties
    {
        get
        {
            return GetDifficultiesByCharacteristic(DifficultyCharacteristic.Standard);
        }
    }

    public List<Difficulty> OneSaberDifficulties
    {
        get
        {
            return GetDifficultiesByCharacteristic(DifficultyCharacteristic.OneSaber);
        }
    }

    public List<Difficulty> NoArrowsDifficulties
    {
        get
        {
            return GetDifficultiesByCharacteristic(DifficultyCharacteristic.NoArrows);
        }
    }

    public List<Difficulty> ThreeSixtyDifficulties
    {
        get
        {
            return GetDifficultiesByCharacteristic(DifficultyCharacteristic.ThreeSixty);
        }
    }

    public List<Difficulty> NinetyDifficulties
    {
        get
        {
            return GetDifficultiesByCharacteristic(DifficultyCharacteristic.Ninety);
        }
    }

    public List<Difficulty> LightshowDifficulties
    {
        get
        {
            return GetDifficultiesByCharacteristic(DifficultyCharacteristic.Lightshow);
        }
    }

    public List<Difficulty> LawlessDifficulties
    {
        get
        {
            return GetDifficultiesByCharacteristic(DifficultyCharacteristic.Lawless);
        }
    }

    public List<Difficulty> UnknownDifficulties
    {
        get
        {
            return GetDifficultiesByCharacteristic(DifficultyCharacteristic.Unknown);
        }
    }


    private BeatmapInfo _info;
    public BeatmapInfo Info
    {
        get
        {
            return _info;
        }
        set
        {
            _info = value;
            OnBeatmapInfoChanged?.Invoke(value);
        }
    }

    private Difficulty _currentMap;
    public Difficulty CurrentMap
    {
        get
        {
            return _currentMap;
        }
        set
        {
            _currentMap = value;
            OnBeatmapDifficultyChanged?.Invoke(value);
        }
    }

    public float DefaultHJD
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

    public float HJD
    {
        get
        {
            return DefaultHJD + CurrentMap.SpawnOffset;
        }
    }
    public float ReactionTime
    {
        get
        {
            return (60 / Info._beatsPerMinute) * HJD;
        }
    }
    public float JumpDistance
    {
        get
        {
            return CurrentMap.NoteJumpSpeed * 2 * ReactionTime;
        }
    }


    public float GetJumpDistance(float HJD, float BPM, float NJS)
    {
        float RT = (60 / BPM) * HJD;
        return NJS * 2 * RT;
    }


    public List<Difficulty> GetDifficultiesByCharacteristic(DifficultyCharacteristic characteristic)
    {
        return Difficulties.FindAll(x => x.characteristic == characteristic);
    }


    public void SetDefaultDifficulty()
    {
        //Load the default difficulty
        //Else if chains my behated (I don't know of a better way to do this)
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


    public delegate void BeatmapInfoDelegate(BeatmapInfo info);
    public event BeatmapInfoDelegate OnBeatmapInfoChanged;

    public delegate void DifficultyDelegate(Difficulty diff);
    public event DifficultyDelegate OnBeatmapDifficultyChanged;


    private void OnEnable()
    {
        if(Instance && Instance != this)
        {
            Debug.Log("Duplicate BeatmapManager in scene.");
            this.enabled = false;
        }
        else Instance = this;

        UIStateManager.OnUIStateChanged += UpdateUIState;
    }


    private void OnDisable()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }
}


public class Difficulty
{
    public DifficultyCharacteristic characteristic;
    public Diff difficultyRank;
    public BeatmapDifficulty beatmapDifficulty;
    public float NoteJumpSpeed;
    public float SpawnOffset;


    public static Difficulty Empty = new Difficulty
    {
        characteristic = DifficultyCharacteristic.Unknown,
        difficultyRank = Diff.ExpertPlus,
        beatmapDifficulty = BeatmapDifficulty.Empty,
        NoteJumpSpeed = 0,
        SpawnOffset = 0
    };
}