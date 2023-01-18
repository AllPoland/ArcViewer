using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class BeatmapLoader : MonoBehaviour
{
    private static bool _loading;
    public static bool Loading
    {
        get
        {
            return _loading;
        }
        private set
        {
            _loading = value;
            OnLoadingChanged?.Invoke(value);
        }
    }

    public delegate void BoolDelegate(bool value);
    public static event BoolDelegate OnLoadingChanged;

    public const string MapFolder = "Map";
    public string MapDirectory = "";

    private BeatmapManager beatmapManager;
    private AudioManager audioManager;


    public static Dictionary<string, Diff> DiffValueFromString = new Dictionary<string, Diff>
    {
        {"Easy", Diff.Easy},
        {"Normal", Diff.Normal},
        {"Hard", Diff.Hard},
        {"Expert", Diff.Expert},
        {"ExpertPlus", Diff.ExpertPlus}
    };


    private IEnumerator LoadMapDirectoryCoroutine(string directory)
    {
        Loading = true;

        Debug.Log("Loading info.");
        Task<BeatmapInfo> infoTask = Task.Run(() => LoadInfoAsync(directory));

        yield return new WaitUntil(() => infoTask.IsCompleted);
        BeatmapInfo info = infoTask.Result;

        Debug.Log("Loading difficulties.");
        Task<List<Difficulty>> diffTask = Task.Run(() => LoadDiffsAsync(directory, info, beatmapManager));
        
        yield return new WaitUntil(() => diffTask.IsCompleted);
        List<Difficulty> difficulties = diffTask.Result;

        string audioDirectory = Path.Combine(directory, info._songFilename);
        AudioClip song = null;
        if(File.Exists(audioDirectory))
        {
            AudioType type = FileUtil.GetAudioTypeByDirectory(audioDirectory);
            Debug.Log($"Loading audio file with type of {type}.");

            using(UnityWebRequest audioUwr = UnityWebRequestMultimedia.GetAudioClip(audioDirectory, type))
            {
                Debug.Log("Loading audio file.");
                audioUwr.SendWebRequest();

                yield return new WaitUntil(() => audioUwr.isDone);
                if(audioUwr.result == UnityWebRequest.Result.ConnectionError || audioUwr.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogWarning($"{audioUwr.error}");
                }
                else
                {
                    song = DownloadHandlerAudioClip.GetContent(audioUwr);
                }
            }
        }
        else
        {
            Debug.LogWarning("Audio file doesn't exist!");
        }

        Debug.Log("Loading complete.");

        UpdateMapInfo(info, difficulties, song);

        Loading = false;
    }


    private IEnumerator LoadMapZipCoroutine(string directory)
    {
        Loading = true;

        Debug.Log("Loading map zip.");
        var loadZipTask = Task.Run(() => ZipReader.GetMapFromZipPathAsync(directory));

        yield return new WaitUntil(() => loadZipTask.IsCompleted);
        var result = loadZipTask.Result;

        BeatmapInfo info = result.Item1;
        List<Difficulty> difficulties = result.Item2;
        TempFile audioFile = result.Item3;

        AudioClip song = null;
        if(audioFile != null && File.Exists(audioFile.Path))
        {
            string audioDirectory = audioFile.Path;
            AudioType type = FileUtil.GetAudioTypeByDirectory(info._songFilename);
            Debug.Log($"Loading audio file with type of {type}.");

            using(UnityWebRequest audioUwr = UnityWebRequestMultimedia.GetAudioClip(audioDirectory, type))
            {
                Debug.Log("Loading audio file.");
                audioUwr.SendWebRequest();

                yield return new WaitUntil(() => audioUwr.isDone);
                if(audioUwr.result == UnityWebRequest.Result.ConnectionError || audioUwr.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogWarning($"{audioUwr.error}");
                }
                else
                {
                    song = DownloadHandlerAudioClip.GetContent(audioUwr);
                }
            }
        }
        else
        {
            Debug.LogWarning("Audio file doesn't exist!");
        }

        Debug.Log("Loading complete.");

        UpdateMapInfo(info, difficulties, song);

        Loading = false;
    }


    private void UpdateMapInfo(BeatmapInfo info, List<Difficulty> difficulties, AudioClip song)
    {
        UIStateManager.CurrentState = UIState.Previewer;
        
        beatmapManager.Info = info;
        beatmapManager.Difficulties = difficulties;
        beatmapManager.SetDefaultDifficulty();

        audioManager.UpdateAudioClip(song);
    }


    public async Task<BeatmapInfo> LoadInfoAsync(string directory)
    {
        string infoPath = Path.Combine(directory, "Info.dat");

        BeatmapInfo info = await JsonReader.LoadInfoAsync(infoPath);
        Debug.Log($"Loaded info for {info._songAuthorName} - {info._songName}, mapped by {info._levelAuthorName}");
        return info;
    }


    public async Task<List<Difficulty>> LoadDiffsAsync(string directory, BeatmapInfo info, BeatmapManager outputBeatmapManager)
    {
        List<Difficulty> difficulties = new List<Difficulty>();

        foreach(DifficultyBeatmapSet set in info._difficultyBeatmapSets)
        {
            if(set._difficultyBeatmaps.Length == 0)
            {
                continue;
            }

            string characteristicName = set._beatmapCharacteristicName;
            DifficultyCharacteristic setCharacteristic = BeatmapInfo.CharacteristicFromString(characteristicName);
            
            List<Difficulty> newDifficulties = new List<Difficulty>();
            foreach(DifficultyBeatmap beatmap in set._difficultyBeatmaps)
            {
                Debug.Log($"Loading {beatmap._beatmapFilename}");
                Difficulty diff = await JsonReader.LoadDifficultyAsync(directory, beatmap);
                diff.characteristic = setCharacteristic;
                newDifficulties.Add(diff);
            }

            difficulties.AddRange(newDifficulties);
            Debug.Log($"Finished loading {newDifficulties.Count} difficulties in characteristic {characteristicName}.");
        }

        return difficulties;
    }


    public void LoadMapDirectory()
    {
        if(Loading)
        {
            Debug.LogWarning("Trying to load a map while already loading!");
            return;
        }

        if(MapDirectory.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            if(!File.Exists(MapDirectory))
            {
                Debug.LogWarning("Trying to load a map from a file that doesn't exist!");
                return;
            }

            StartCoroutine(LoadMapZipCoroutine(MapDirectory));
            return;
        }

        if(!Directory.Exists(MapDirectory))
        {
            Debug.LogWarning("Trying to load a map from a directory that doesn't exist!");
            return;
        }

        StartCoroutine(LoadMapDirectoryCoroutine(MapDirectory));
    }


    public void UpdateDirectory(string newDirectory)
    {
        MapDirectory = newDirectory;
    }


    private void Start()
    {
        beatmapManager = BeatmapManager.Instance;
        audioManager = AudioManager.Instance;
    }
}