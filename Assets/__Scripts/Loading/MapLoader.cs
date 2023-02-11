using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

public class MapLoader : MonoBehaviour
{
    private static bool _loading;
    public static bool Loading
    {
        get => _loading;

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


    public static Dictionary<string, DifficultyRank> DiffValueFromString = new Dictionary<string, DifficultyRank>
    {
        {"Easy", DifficultyRank.Easy},
        {"Normal", DifficultyRank.Normal},
        {"Hard", DifficultyRank.Hard},
        {"Expert", DifficultyRank.Expert},
        {"ExpertPlus", DifficultyRank.ExpertPlus}
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
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Unable to load Info.dat!");
            UpdateMapInfo(null, new List<Difficulty>(), null, null);
            yield break;
        }

        Debug.Log("Loading difficulties.");
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
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Unable to load audio file!");
            Debug.LogWarning("Song file not found!");
        }

        Debug.Log("Loading cover image.");
        LoadingMessage = "Loading cover image";

        Task<Stream> coverImageTask = Task.Run(() => FileUtil.ReadFileData(Path.Combine(directory, info._coverImageFilename)));
        yield return new WaitUntil(() => coverImageTask.IsCompleted);
        Stream coverImageStream = coverImageTask.IsCanceled ? null : coverImageTask.Result;

        byte[] coverImageData = new byte[0];
        if(coverImageStream == null)
        {
            ErrorHandler.Instance.DisplayPopup(ErrorType.Warning, "Cover image not found!");
            Debug.Log($"Didn't find image file {info._coverImageFilename}!");
        }
        else
        {
            coverImageData = FileUtil.StreamToBytes(coverImageStream);
            coverImageStream.Dispose();
        }

        Debug.Log("Loading complete.");
        LoadingMessage = "Done";

        UpdateMapInfo(info, difficulties, song, coverImageData);
    }


    private IEnumerator LoadMapZipArchiveCoroutine(ZipArchive archive)
    {
        Debug.Log("Loading map zip.");
        LoadingMessage = "Loading map zip";
        var loadZipTask = Task.Run(() => ZipReader.GetMapFromZipArchiveAsync(archive));

        yield return new WaitUntil(() => loadZipTask.IsCompleted);
        var result = loadZipTask.Result;
        BeatmapInfo info = result.Item1;
        List<Difficulty> difficulties = result.Item2;
        TempFile audioFile = result.Item3;
        byte[] coverImageData = result.Item4;

        archive.Dispose();

        if(info == null)
        {
            UpdateMapInfo(info, difficulties, null, coverImageData);
            yield break;
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
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Unable to load audio file!");
            Debug.LogWarning("Audio file doesn't exist!");
        }

        Debug.Log("Loading complete.");
        LoadingMessage = "Done";

        UpdateMapInfo(info, difficulties, song, coverImageData);
    }


    private void LoadMapZip(string directory)
    {
        Loading = true;

        ZipArchive archive = null;
        try
        {
            archive = ZipFile.OpenRead(directory);
            StartCoroutine(LoadMapZipArchiveCoroutine(archive));
        }
        catch(Exception err)
        {
            if(archive != null)
            {
                archive.Dispose();
            }

            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Failed to load the zip file!");
            Debug.LogWarning($"Unhandled exception loading zip: {err.Message}, {err.StackTrace}.");
        }
    }


    private IEnumerator LoadMapURLCoroutine(string url, string mapID)
    {
        Loading = true;

        //Use the map id as the map key to avoid making requests to beatsaver for cached maps
        //This method of doing this is pretty yucky but it gets the job done so fuck it
        string cacheKey = mapID != "" ? mapID : url;
        string cachedMapPath = FileCache.GetCachedFile(cacheKey);
        if(cachedMapPath != null && cachedMapPath != "")
        {
            Debug.Log("Found map in cache.");
            LoadMapZip(cachedMapPath);
            yield break;
        }

        Debug.Log("Downloading map data.");
        LoadingMessage = "Downloading map";
        Task<Stream> downloadTask = Task.Run(() => WebLoader.LoadMapURL(url));

        while(!downloadTask.IsCompleted)
        {
            Progress = WebLoader.Progress;
            yield return null;
        }
        Stream zipStream = downloadTask.Result;
        Progress = 0;

        if(zipStream == null)
        {
            UpdateMapInfo(null, new List<Difficulty>(), null, null);
            yield break;
        }

        FileCache.SaveFileToCache(zipStream, cacheKey);
        ZipArchive archive = null;
        
        try
        {
            archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            StartCoroutine(LoadMapZipArchiveCoroutine(archive));
        }
        catch(Exception err)
        {
            if(archive != null)
            {
                archive.Dispose();
            }

            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Failed to download map file!");
            Debug.LogWarning($"Unhandled exception loading zip URL: {err.Message}, {err.StackTrace}");
        }
    }


    private IEnumerator LoadMapIDCoroutine(string mapID)
    {
        Loading = true;

        string cachedMapPath = FileCache.GetCachedFile(mapID);
        if(cachedMapPath != null && cachedMapPath != "")
        {
            Debug.Log("Found map in cache.");
            LoadMapZip(cachedMapPath);
            yield break;
        }

        Debug.Log("Getting beat saver response");
        LoadingMessage = "Fetching map from BeatSaver";
        Task<string> apiTask = Task.Run(() => BeatSaverHandler.GetBeatSaverMapURL(mapID));

        yield return new WaitUntil(() => apiTask.IsCompleted);
        string mapURL = apiTask.Result;

        if(mapURL == null || mapURL == "")
        {
            Debug.Log("Empty or nonexistant URL!");
            UpdateMapInfo(null, new List<Difficulty>(), null, null);
            yield break;
        }

        StartCoroutine(LoadMapURLCoroutine(mapURL, mapID));
    }


    private void UpdateMapInfo(BeatmapInfo info, List<Difficulty> difficulties, AudioClip song, byte[] coverData)
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

        UIStateManager.CurrentState = UIState.Previewer;
        
        BeatmapManager.Info = info;
        AudioManager.Instance.MusicClip = song;

        if(coverData != null && coverData.Length > 0)
        {
            CoverImageHandler.Instance.SetImageFromData(coverData);
        }
        else CoverImageHandler.Instance.SetDefaultImage();

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
            string characteristicName = set._beatmapCharacteristicName;

            if(set._difficultyBeatmaps.Length == 0)
            {
                Debug.LogWarning($"{characteristicName} lists no difficulties!");
                continue;
            }

            DifficultyCharacteristic setCharacteristic = BeatmapInfo.CharacteristicFromString(characteristicName);
            
            List<Difficulty> newDifficulties = new List<Difficulty>();
            foreach(DifficultyBeatmap beatmap in set._difficultyBeatmaps)
            {
                LoadingMessage = $"Loading {beatmap._beatmapFilename}";
                Debug.Log($"Loading {beatmap._beatmapFilename}");

                Difficulty diff = await JsonReader.LoadDifficultyAsync(directory, beatmap);
                if(diff == null)
                {
                    Debug.LogWarning($"Unable to load {beatmap._beatmapFilename}!");
                    continue;
                }

                diff.characteristic = setCharacteristic;
                diff.requirements = beatmap._customData?._requirements ?? new string[0];

                newDifficulties.Add(diff);
            }

            difficulties.AddRange(newDifficulties);
            Debug.Log($"Finished loading {newDifficulties.Count} difficulties in characteristic {characteristicName}.");
        }

        return difficulties;
    }


    public void LoadMapDirectory(string mapDirectory)
    {
        if(DialogueHandler.DialogueActive)
        {
            return;
        }

        if(Loading)
        {
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "You're already loading something!");
            Debug.LogWarning("Trying to load a map while already loading!");
            return;
        }

        if(mapDirectory.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if(mapDirectory.Contains("beatsaver.com/maps"))
            {
                //Direct beatsaver link, should load based on ID instead
                string ID = mapDirectory.Split("/").Last();
                StartCoroutine(LoadMapIDCoroutine(ID));
                return;
            }

            StartCoroutine(LoadMapURLCoroutine(mapDirectory, ""));
            return;
        }

        if(mapDirectory.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            if(!File.Exists(mapDirectory))
            {
                ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "That file doesn't exist!");
                Debug.LogWarning("Trying to load a map from a file that doesn't exist!");
                return;
            }

            LoadMapZip(mapDirectory);
            return;
        }

        const string IDchars = "0123456789abcdef";
        //If the directory doesn't contain any characters that aren't hexadecimal, that means it's probably an ID
        if(!mapDirectory.Any(x => !IDchars.Contains(x)))
        {
            StartCoroutine(LoadMapIDCoroutine(mapDirectory));
            return;
        }

        if(!Directory.Exists(mapDirectory))
        {
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "That directory doesn't exist!");
            Debug.LogWarning("Trying to load a map from a directory that doesn't exist!");
            return;
        }

        StartCoroutine(LoadMapDirectoryCoroutine(mapDirectory));
    }
}