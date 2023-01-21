using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using UnityEngine;

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

    public static int Progress = 0;
    public static string LoadingMessage = "";

    public delegate void BoolDelegate(bool value);
    public static event BoolDelegate OnLoadingChanged;

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
        LoadingMessage = "Loading Info.dat";
        Task<BeatmapInfo> infoTask = Task.Run(() => LoadInfoAsync(directory));

        yield return new WaitUntil(() => infoTask.IsCompleted);
        BeatmapInfo info = infoTask.Result;

        if(info == null)
        {
            UpdateMapInfo(null, new List<Difficulty>(), null);
            yield break;
        }

        Debug.Log("Loading difficulties.");
        LoadingMessage = "Loading difficulty files";
        Task<List<Difficulty>> diffTask = Task.Run(() => LoadDiffsAsync(directory, info));
        
        yield return new WaitUntil(() => diffTask.IsCompleted);
        List<Difficulty> difficulties = diffTask.Result;

        LoadingMessage = "Loading song";
        string audioDirectory = Path.Combine(directory, info._songFilename);
        AudioClip song = null;
        if(File.Exists(audioDirectory))
        {
            AudioType type = FileUtil.GetAudioTypeByDirectory(audioDirectory);
            Debug.Log($"Loading audio file with type of {type}.");

            Task<AudioClip> audioTask = FileUtil.GetAudioFromFile(audioDirectory, type);
            yield return new WaitUntil(() => audioTask.IsCompleted);

            song = audioTask.Result;
        }
        else
        {
            Debug.LogWarning("Audio file doesn't exist!");
        }

        Debug.Log("Loading complete.");
        LoadingMessage = "Done";

        UpdateMapInfo(info, difficulties, song);
    }


    private IEnumerator LoadMapZipCoroutine(string directory)
    {
        Loading = true;

        Debug.Log("Loading map zip.");
        LoadingMessage = "Reading map zip";
        var loadZipTask = Task.Run(() => ZipReader.GetMapFromZipPathAsync(directory));

        yield return new WaitUntil(() => loadZipTask.IsCompleted);
        var result = loadZipTask.Result;

        BeatmapInfo info = result.Item1;
        List<Difficulty> difficulties = result.Item2;
        TempFile audioFile = result.Item3;

        LoadingMessage = "Loading song";
        AudioClip song = null;
        if(audioFile != null && File.Exists(audioFile.Path))
        {
            AudioType type = FileUtil.GetAudioTypeByDirectory(info._songFilename);
            Debug.Log($"Loading audio file with type of {type}.");

            Task<AudioClip> audioTask = FileUtil.GetAudioFromFile(audioFile.Path, type);
            yield return new WaitUntil(() => audioTask.IsCompleted);

            song = audioTask.Result;
        }
        else
        {
            Debug.LogWarning("Audio file doesn't exist!");
        }

        Debug.Log("Loading complete.");
        LoadingMessage = "Done";

        UpdateMapInfo(info, difficulties, song);
    }


    private IEnumerator LoadMapURLCoroutine(string url)
    {
        Loading = true;

        Debug.Log("Downloading map data.");
        LoadingMessage = "Downloading map";
        Task<Stream> downloadTask = Task.Run(() => WebMapLoader.LoadMapURL(url));

        while(!downloadTask.IsCompleted)
        {
            Progress = WebMapLoader.Progress;
            yield return null;
        }
        Stream zipStream = downloadTask.Result;
        Progress = 0;

        if(zipStream == null)
        {
            Debug.LogWarning("Download failed!");
            UpdateMapInfo(null, new List<Difficulty>(), null);
            yield break;
        }

        LoadingMessage = "Loading map zip";
        BeatmapInfo info = null;
        List<Difficulty> difficulties = new List<Difficulty>();
        TempFile audioFile = null;
        using(ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
        {
            Debug.Log("Loading map zip.");
            var loadZipTask = Task.Run(() => ZipReader.GetMapFromZipArchiveAsync(archive));

            yield return new WaitUntil(() => loadZipTask.IsCompleted);
            var result = loadZipTask.Result;
            info = result.Item1;
            difficulties = result.Item2;
            audioFile = result.Item3;
        }

        LoadingMessage = "Loading song";
        AudioClip song = null;
        if(audioFile != null && File.Exists(audioFile.Path))
        {
            AudioType type = FileUtil.GetAudioTypeByDirectory(info._songFilename);
            Debug.Log($"Loading audio file with type of {type}.");

            Task<AudioClip> audioTask = FileUtil.GetAudioFromFile(audioFile.Path, type);
            yield return new WaitUntil(() => audioTask.IsCompleted);

            song = audioTask.Result;
        }
        else
        {
            Debug.LogWarning("Audio file doesn't exist!");
        }

        Debug.Log("Loading complete.");
        LoadingMessage = "Done";

        UpdateMapInfo(info, difficulties, song);
    }


    private IEnumerator LoadMapIDCoroutine(string mapID)
    {
        Loading = true;

        Debug.Log("Getting beat saver response");
        LoadingMessage = "Fetching map from BeatSaver";
        Task<string> apiTask = Task.Run(() => BeatSaverHandler.GetBeatSaverMapURL(mapID));

        yield return new WaitUntil(() => apiTask.IsCompleted);
        string mapURL = apiTask.Result;

        StartCoroutine(LoadMapURLCoroutine(mapURL));
    }


    private void UpdateMapInfo(BeatmapInfo info, List<Difficulty> difficulties, AudioClip song)
    {
        StopAllCoroutines();
        Progress = 0;
        LoadingMessage = "";
        Loading = false;
        
        if(info == null || difficulties.Count == 0 || song == null)
        {
            Debug.LogWarning("Failed to load map file");
            return;
        }

        audioManager.UpdateAudioClip(song);
        UIStateManager.CurrentState = UIState.Previewer;
        
        BeatmapManager.Info = info;
        BeatmapManager.Difficulties = difficulties;
        BeatmapManager.SetDefaultDifficulty();
    }


    public async Task<BeatmapInfo> LoadInfoAsync(string directory)
    {
        string infoPath = Path.Combine(directory, "Info.dat");

        BeatmapInfo info = await JsonReader.LoadInfoAsync(infoPath);
        if(info == null) return null;

        Debug.Log($"Loaded info for {info._songAuthorName} - {info._songName}, mapped by {info._levelAuthorName}");
        return info;
    }


    public async Task<List<Difficulty>> LoadDiffsAsync(string directory, BeatmapInfo info)
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


    public void LoadMapDirectory(string mapDirectory)
    {
        if(mapDirectory == "")
        {
            return;
        }

        if(Loading)
        {
            Debug.LogWarning("Trying to load a map while already loading!");
            return;
        }

        if(mapDirectory.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            StartCoroutine(LoadMapURLCoroutine(mapDirectory));
            return;
        }

        if(mapDirectory.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            if(!File.Exists(mapDirectory))
            {
                Debug.LogWarning("Trying to load a map from a file that doesn't exist!");
                return;
            }

            StartCoroutine(LoadMapZipCoroutine(mapDirectory));
            return;
        }

        if(!mapDirectory.Contains(Path.DirectorySeparatorChar) && !mapDirectory.Contains(Path.AltDirectorySeparatorChar))
        {
            StartCoroutine(LoadMapIDCoroutine(mapDirectory));
            return;
        }

        if(!Directory.Exists(mapDirectory))
        {
            Debug.LogWarning("Trying to load a map from a directory that doesn't exist!");
            return;
        }

        StartCoroutine(LoadMapDirectoryCoroutine(mapDirectory));
    }


    private void Start()
    {
        audioManager = AudioManager.Instance;
    }
}