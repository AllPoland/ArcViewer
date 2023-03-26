using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class UrlArgHandler : MonoBehaviour
{
    public const string ArcViewerName = "ArcViewer";
    public const string ArcViewerURL = "https://allpoland.github.io/ArcViewer/";

    [DllImport("__Internal")]
    public static extern string GetParameters();

    [DllImport("__Internal")]
    public static extern void SetPageTitle(string title);

    private static string _loadedMapID;
    public static string LoadedMapID
    {
        get => _loadedMapID;

        set
        {
            _loadedMapID = value;
            _loadedMapURL = null;
        }
    }

    private static string _loadedMapURL;
    public static string LoadedMapURL
    {
        get => _loadedMapURL;

        set
        {
            _loadedMapURL = value;
            _loadedMapID = null;
        }
    }

    public static DifficultyCharacteristic? LoadedCharacteristic;
    public static DifficultyRank? LoadedDiffRank;

    private static string mapID;
    private static string mapURL;
    private static float startTime;
    private static DifficultyCharacteristic? mode;
    private static DifficultyRank? diffRank;

    [SerializeField] private MapLoader mapLoader;


    public void LoadMapFromParameters(string parameters)
    {
        mapID = "";
        mapURL = "";
        startTime = 0;
        mode = null;
        diffRank = null;

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
                    mapID = value;
                    break;
                case "url":
                    mapURL = value;
                    break;
                case "t":
                    success = float.TryParse(value, out startTime);
                    if(!success) startTime = 0;
                    break;
                case "mode":
                    DifficultyCharacteristic parsedMode;
                    success = Enum.TryParse(value, true, out parsedMode);
                    mode = success ? parsedMode : null;
                    break;
                case "difficulty":
                    DifficultyRank parsedRank;
                    success = Enum.TryParse(value, true, out parsedRank);
                    diffRank = success ? parsedRank : null;
                    break;
            }
        }

        bool autoLoad = false;
        if(!string.IsNullOrEmpty(mapID))
        {
            StartCoroutine(mapLoader.LoadMapIDCoroutine(mapID));
            LoadedMapID = mapID;

            autoLoad = true;
        }
        else if(!string.IsNullOrEmpty(mapURL))
        {
            StartCoroutine(mapLoader.LoadMapURLCoroutine(mapURL));
            LoadedMapURL = mapURL;

            autoLoad = true;
        }

        if(autoLoad)
        {
            //Only apply start time and diff when a map is also included in the arguments
            if(startTime > 0)
            {
                MapLoader.OnMapLoaded += SetTime;
            }

            if(mode != null || diffRank != null)
            {
                MapLoader.OnMapLoaded += SetDifficulty;
            }
        }
    }


    public void SetTime()
    {
        TimeManager.CurrentTime = startTime;
        MapLoader.OnMapLoaded -= SetTime;
    }


    public void SetDifficulty()
    {
        if(mode != null)
        {
            //Since mode is nullable I have to cast it (cringe)
            DifficultyCharacteristic characteristic = (DifficultyCharacteristic)mode;

            List<Difficulty> difficulties = BeatmapManager.GetDifficultiesByCharacteristic(characteristic);
            Difficulty difficulty = null;

            if(diffRank != null)
            {
                difficulty = difficulties.FirstOrDefault(x => x.difficultyRank == diffRank);
            }
            BeatmapManager.CurrentDifficulty = difficulty ?? difficulties.Last();
        }
        else if(diffRank != null)
        {
            DifficultyCharacteristic defaultCharacteristic = BeatmapManager.GetDefaultDifficulty().characteristic;
            List<Difficulty> difficulties = BeatmapManager.GetDifficultiesByCharacteristic(defaultCharacteristic);

            Difficulty difficulty = difficulties.FirstOrDefault(x => x.difficultyRank == diffRank);
            BeatmapManager.CurrentDifficulty = difficulty ?? difficulties.Last();
        }
        MapLoader.OnMapLoaded -= SetDifficulty;
    }


    public void ClearSubscriptions()
    {
        MapLoader.OnMapLoaded -= SetTime;
        MapLoader.OnMapLoaded -= SetDifficulty;
    }


    public void UpdateLoadedDifficulty(Difficulty newDifficulty)
    {
        Difficulty defaultDifficulty = BeatmapManager.GetDefaultDifficulty();
        if(newDifficulty == defaultDifficulty)
        {
            //No need to specify for the default difficulty
            LoadedCharacteristic = null;
            LoadedDiffRank = null;
            return;
        }

        LoadedCharacteristic = newDifficulty.characteristic;
        LoadedDiffRank = newDifficulty.difficultyRank;
    }


#if UNITY_WEBGL && !UNITY_EDITOR
    public void UpdateMapTitle(BeatmapInfo info)
    {
        string mapTitle = "";
        if(info != BeatmapInfo.Empty)
        {
            string authorName = info._songAuthorName;
            string songName = info._songName;
            if(!string.IsNullOrEmpty(authorName))
            {
                mapTitle += authorName;
                if(!string.IsNullOrEmpty(songName))
                {
                    //Add a separator when there's an author and and song name
                    //(This will be the case 99% of the time)
                    mapTitle += " - ";
                }
            }
            mapTitle += songName;

            if(!string.IsNullOrEmpty(mapTitle))
            {
                //Add a separator between the webpage title and map title
                mapTitle = " | " + mapTitle;
            }
        }

        SetPageTitle($"{ArcViewerName}{mapTitle}");
    }
#endif


    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string parameters = GetParameters();

        if(!string.IsNullOrEmpty(parameters))
        {
            LoadMapFromParameters(parameters);
        }

        BeatmapManager.OnBeatmapInfoChanged += UpdateMapTitle;
#endif
        MapLoader.OnLoadingFailed += ClearSubscriptions;
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateLoadedDifficulty;
    }
}