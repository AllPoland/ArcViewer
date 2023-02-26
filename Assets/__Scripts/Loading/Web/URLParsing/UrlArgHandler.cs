using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class UrlArgHandler : MonoBehaviour
{
    [DllImport("__Internal")]
    public static extern string GetParameters();

    public static string MapID;
    public static string MapURL;
    public static float StartTime;
    public static DifficultyCharacteristic? Mode;
    public static DifficultyRank? DiffRank;

    [SerializeField] private MapLoader mapLoader;


    public void LoadMapFromParameters(string parameters)
    {
        MapID = "";
        MapURL = "";
        StartTime = 0;
        Mode = null;
        DiffRank = null;

        if(MapLoader.Loading) return;

        //Remove the ? from the start of the parameters
        parameters = parameters.TrimStart('?');

        string[] args = parameters.Split('&');
        if(args.Length <= 0) return;

        for(int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            //Check for a single = in the argument
            if(arg.Count(x => x == '=') != 1)
            {
                continue;
            }

            //Split the argument into its name and value
            string[] elements = arg.Split('=');
            string name = elements[0];
            string value = elements[1];

            //Define this bool here because for some reason each case shares scope
            bool success = false;
            switch(name)
            {
                case "id":
                    MapID = value;
                    break;
                case "url":
                    MapURL = value;
                    break;
                case "t":
                    success = float.TryParse(value, out StartTime);
                    if(!success) StartTime = 0;
                    break;
                case "mode":
                    DifficultyCharacteristic parsedMode;
                    success = Enum.TryParse(value, true, out parsedMode);
                    Mode = success ? parsedMode : null;
                    break;
                case "difficulty":
                    DifficultyRank parsedRank;
                    success = Enum.TryParse(value, true, out parsedRank);
                    DiffRank = success ? parsedRank : null;
                    break;
            }
        }

        if(StartTime > 0)
        {
            MapLoader.OnMapLoaded += SetTime;
        }

        if(Mode != null || DiffRank != null)
        {
            MapLoader.OnMapLoaded += SetDifficulty;
        }

        if(!string.IsNullOrEmpty(MapID))
        {
            StartCoroutine(mapLoader.LoadMapIDCoroutine(MapID));
        }
        else if(!string.IsNullOrEmpty(MapURL))
        {
            StartCoroutine(mapLoader.LoadMapURLCoroutine(MapURL));
        }
    }


    public void SetTime()
    {
        TimeManager.CurrentTime = StartTime;

        StartTime = 0;
        MapLoader.OnMapLoaded -= SetTime;
    }


    public void SetDifficulty()
    {
        if(Mode != null)
        {
            //Since Mode is nullable I have to cast it (cringe)
            DifficultyCharacteristic characteristic = (DifficultyCharacteristic)Mode;

            List<Difficulty> difficulties = BeatmapManager.GetDifficultiesByCharacteristic(characteristic);
            Difficulty difficulty = null;

            if(DiffRank != null)
            {
                difficulty = difficulties.FirstOrDefault(x => x.difficultyRank == DiffRank);
            }
            BeatmapManager.CurrentMap = difficulty ?? difficulties.Last();
        }
        else if(DiffRank != null)
        {
            DifficultyCharacteristic defaultCharacteristic = BeatmapManager.GetDefaultDifficulty().characteristic;
            List<Difficulty> difficulties = BeatmapManager.GetDifficultiesByCharacteristic(defaultCharacteristic);

            Difficulty difficulty = difficulties.FirstOrDefault(x => x.difficultyRank == DiffRank);
            BeatmapManager.CurrentMap = difficulty ?? difficulties.Last();
        }

        Mode = null;
        DiffRank = null;
        MapLoader.OnMapLoaded -= SetDifficulty;
    }


    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string parameters = GetParameters();
        Debug.Log(parameters);

        if(!string.IsNullOrEmpty(parameters))
        {
            LoadMapFromParameters(parameters);
        }
#endif
    }
}