using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class MapLoader : MonoBehaviour
{
    private static bool _loading = false;
    public static bool Loading
    {
        get => _loading;

        private set
        {
            _loading = value;
            OnLoadingChanged?.Invoke(value);
        }
    }

    public static string LoadingMessage = "";
    public static float Progress;

    public static event Action<bool> OnLoadingChanged;
    public static event Action OnMapLoaded;
    public static event Action OnLoadingFailed;


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
        //Loading maps from directories will never work in WebGL
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.LogWarning("Tried to load from directory in WebGL!");
        ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Loading from directory doesn't work in browser!");

        UpdateMapInfo(LoadedMap.Empty);
        yield break;
#else
        Loading = true;

        Debug.Log("Loading info.");
        LoadingMessage = "Loading Info.dat";
        using Task<BeatmapInfo> infoTask = Task.Run(() => LoadInfoAsync(directory));

        yield return new WaitUntil(() => infoTask.IsCompleted);
        BeatmapInfo info = infoTask.Result;

        if(info == null)
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Unable to load Info.dat!");
            UpdateMapInfo(LoadedMap.Empty);
            yield break;
        }

        Debug.Log("Loading difficulties.");
        using Task<List<Difficulty>> diffTask = Task.Run(() => LoadDiffsAsync(directory, info));
        
        yield return new WaitUntil(() => diffTask.IsCompleted);
        List<Difficulty> difficulties = diffTask.Result;

        LoadingMessage = "Loading song";
        string audioDirectory = Path.Combine(directory, info._songFilename);
        AudioClip song = null;
        if(File.Exists(audioDirectory))
        {
            using Task<AudioType> typeTask = AudioFileHandler.GetAudioTypeFromFile(audioDirectory);
            yield return new WaitUntil(() => typeTask.IsCompleted);

            AudioType type = typeTask.Result;
            Debug.Log($"Loading audio file with type of {type}.");

            using Task<AudioClip> audioTask = AudioFileHandler.GetAudioFromFile(audioDirectory, type);
            yield return new WaitUntil(() => audioTask.IsCompleted);

            song = audioTask.Result;
        }
        else
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Unable to load audio file!");
            Debug.LogWarning("Song file not found!");

            UpdateMapInfo(LoadedMap.Empty);
            yield break;
        }

        if(song == null)
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Unable to load audio file!");
            Debug.LogWarning("Song file not found!");

            UpdateMapInfo(LoadedMap.Empty);
            yield break;
        }

        Debug.Log("Loading cover image.");
        LoadingMessage = "Loading cover image";

        using Task<Stream> coverImageTask = Task.Run(() => FileUtil.ReadFileData(Path.Combine(directory, info._coverImageFilename)));
        yield return new WaitUntil(() => coverImageTask.IsCompleted);
        Stream coverImageStream = coverImageTask.IsCompletedSuccessfully ? coverImageTask.Result : null;

        byte[] coverImageData = new byte[0];
        if(coverImageStream == null)
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Warning, "Cover image not found!");
            Debug.Log($"Didn't find image file {info._coverImageFilename}!");
        }
        else
        {
            coverImageData = FileUtil.StreamToBytes(coverImageStream);
            coverImageStream.Dispose();
        }

        Debug.Log("Loading complete.");
        LoadingMessage = "Done";

        UpdateMapInfo(new LoadedMap(info, difficulties, coverImageData, song));
#endif
    }


    private IEnumerator LoadMapZipArchiveCoroutine(ZipArchive archive, Stream zipStream = null)
    {
        Debug.Log("Loading map zip.");
        LoadingMessage = "Loading map zip";

#if !UNITY_WEBGL || UNITY_EDITOR
        using Task<(BeatmapInfo, List<Difficulty>, MemoryStream, byte[])> loadZipTask = Task.Run(() => ZipReader.GetMapFromZipArchiveAsync(archive));
#else
        using Task<(BeatmapInfo, List<Difficulty>, MemoryStream, byte[])> loadZipTask = ZipReader.GetMapFromZipArchiveAsync(archive);
#endif

        yield return new WaitUntil(() => loadZipTask.IsCompleted);
        var result = loadZipTask.Result;
        BeatmapInfo info = result.Item1;
        List<Difficulty> difficulties = result.Item2;
        MemoryStream songData = result.Item3;
        byte[] coverImageData = result.Item4;

        loadZipTask.Dispose();

        if(info == null)
        {
            DisposeZip();

            UpdateMapInfo(LoadedMap.Empty);
            yield break;
        }

        if(songData == null)
        {
            DisposeZip();

            UpdateMapInfo(LoadedMap.Empty);
            yield break;
        }

        LoadingMessage = "Loading song";

#if !UNITY_WEBGL || UNITY_EDITOR
        //Write song data to a tempfile so it can be loaded through a uwr
        using TempFile songFile = new TempFile();
        using(Task writeTask = File.WriteAllBytesAsync(songFile.Path, songData.ToArray()))
        {
            yield return new WaitUntil(() => writeTask.IsCompleted);
        }

        AudioClip song = null;
        if(File.Exists(songFile.Path))
        {
            using Task<AudioType> typeTask = AudioFileHandler.GetAudioTypeFromFile(info._songFilename, songFile.Path);
            yield return new WaitUntil(() => typeTask.IsCompleted);

            AudioType type = typeTask.Result;
            Debug.Log($"Loading audio file with type of {type}.");

            Task<AudioClip> audioTask = AudioFileHandler.GetAudioFromFile(songFile.Path, type);
            yield return new WaitUntil(() => audioTask.IsCompleted);

            song = audioTask.Result;
            audioTask.Dispose();
        }
        else
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Unable to load audio file!");
            Debug.LogWarning("Song file not found!");

            DisposeZip();

            UpdateMapInfo(LoadedMap.Empty);
            yield break;
        }
#else
        Debug.Log("Loading audio file.");
        WebAudioClip song = null;
        using(Task<WebAudioClip> audioTask = AudioFileHandler.WebAudioClipFromStream(songData))
        {
            yield return new WaitUntil(() => audioTask.IsCompleted);

            song = audioTask.Result;
        }
#endif

        if(song == null)
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Unable to load audio file!");
            Debug.LogWarning("Failed to load AudioClip!");

            DisposeZip();

            UpdateMapInfo(LoadedMap.Empty);
            yield break;
        }

        Debug.Log("Loading complete.");
        LoadingMessage = "Done";

        DisposeZip();
        UpdateMapInfo(new LoadedMap(info, difficulties, coverImageData, song));

        void DisposeZip()
        {
            zipStream?.Dispose();
            songData?.Dispose();
            archive?.Dispose();
        }
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
            archive?.Dispose();

            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Failed to load zip file!");
            Debug.LogWarning($"Unhandled exception loading zip: {err.Message}, {err.StackTrace}.");

            UpdateMapInfo(LoadedMap.Empty);
        }
    }


#if UNITY_WEBGL && !UNITY_EDITOR
    private IEnumerator LoadMapZipWebGLCoroutine(string directory)
    {
        Loading = true;
        LoadingMessage = "Loading zip";

        Stream zipStream = null;
        ZipArchive archive = null;

        using UnityWebRequest uwr = UnityWebRequest.Get(directory);
        
        Debug.Log("Starting web request.");
        yield return uwr.SendWebRequest();

        if(uwr.result == UnityWebRequest.Result.Success)
        {
            try
            {
                zipStream = new MemoryStream(uwr.downloadHandler.data);
                archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

                StartCoroutine(LoadMapZipArchiveCoroutine(archive, zipStream));
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to read map data with error: {e.Message}, {e.StackTrace}");
                ErrorHandler.Instance.ShowPopup(ErrorType.Error, $"Failed to read map data!");

                Dispose();

                UpdateMapInfo(LoadedMap.Empty);
                yield break;
            }
        }
        else
        {
            Debug.LogWarning(uwr.error);
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, $"Failed to load map! {uwr.error}");

            Dispose();

            UpdateMapInfo(LoadedMap.Empty);
            yield break;
        }

        void Dispose()
        {
            zipStream?.Dispose();
            archive?.Dispose();
        }
    }


    public void LoadMapZipWebGL(string directory)
    {
        if(DialogueHandler.DialogueActive)
        {
            return;
        }

        if(Loading)
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "You're already loading something!");
            Debug.LogWarning("Trying to load a map while already loading!");
            return;
        }

        StartCoroutine(LoadMapZipWebGLCoroutine(directory));
        UrlArgHandler.LoadedMapURL = null;
    }
#endif


    public IEnumerator LoadMapURLCoroutine(string url, string mapID = null)
    {
        Loading = true;

#if !UNITY_WEBGL || UNITY_EDITOR
        //Use the map id as the map key to avoid making requests to beatsaver for cached maps
        string cacheKey = string.IsNullOrEmpty(mapID) ? url : mapID;
        string cachedMapPath = FileCache.GetCachedFile(cacheKey);

        if(cachedMapPath != null && cachedMapPath != "")
        {
            Debug.Log("Found map in cache.");
            LoadMapZip(cachedMapPath);
            yield break;
        }
#endif

        Debug.Log($"Downloading map data from: {url}");
        LoadingMessage = "Downloading map";

        using Task<Stream> downloadTask = WebLoader.LoadMapURL(url);
        yield return new WaitUntil(() => downloadTask.IsCompleted);

        Stream zipStream = downloadTask.Result;

        if(zipStream == null)
        {
            Debug.LogWarning("Downloaded data is null!");

            UpdateMapInfo(LoadedMap.Empty);
            yield break;
        }
#if !UNITY_WEBGL || UNITY_EDITOR
        else
        {
            FileCache.SaveFileToCache(zipStream, cacheKey);
        }
#endif

        ZipArchive archive = null;
        
        try
        {
            archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            StartCoroutine(LoadMapZipArchiveCoroutine(archive, zipStream));
        }
        catch(Exception err)
        {
            archive?.Dispose();
            zipStream?.Dispose();

            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Failed to read map zip!");
            Debug.LogWarning($"Unhandled exception loading zip URL: {err.Message}, {err.StackTrace}");

            UpdateMapInfo(LoadedMap.Empty);
        }
    }


    public IEnumerator LoadMapIDCoroutine(string mapID)
    {
        Loading = true;

#if !UNITY_WEBGL || UNITY_EDITOR
        string cachedMapPath = FileCache.GetCachedFile(mapID);
        if(cachedMapPath != null && cachedMapPath != "")
        {
            Debug.Log("Found map in cache.");
            LoadMapZip(cachedMapPath);
            yield break;
        }
#endif

        Debug.Log($"Getting beat saver response for ID: {mapID}");
        LoadingMessage = "Fetching map from BeatSaver";

        using Task<string> apiTask = BeatSaverHandler.GetBeatSaverMapURL(mapID);
        yield return new WaitUntil(() => apiTask.IsCompleted);
        
        string mapURL = apiTask.Result;

        if(mapURL == null || mapURL == "")
        {
            Debug.Log("Empty or nonexistant URL!");
            UpdateMapInfo(LoadedMap.Empty);
            yield break;
        }

        StartCoroutine(LoadMapURLCoroutine(mapURL, mapID));
    }

    private void UpdateMapInfo(LoadedMap newMap)
    {
        StopAllCoroutines();
        LoadingMessage = "";
        Loading = false;
        
        if(newMap.Info == null || newMap.Difficulties.Count == 0 || newMap.Song == null)
        {
            Debug.LogWarning("Failed to load map file");

            if(newMap.Song != null)
            {
#if !UNITY_WEBGL || UNITY_EDITOR
                newMap.Song.UnloadAudioData();
                Destroy(newMap.Song);
#else
                newMap.Song.Dispose();
#endif
            }
            UIStateManager.CurrentState = UIState.MapSelection;
            OnLoadingFailed?.Invoke();

            return;
        }

        UIStateManager.CurrentState = UIState.Previewer;
        
        BeatmapManager.Info = newMap.Info;
        AudioManager.Instance.MusicClip = newMap.Song;

        if(newMap.CoverImageData != null && newMap.CoverImageData.Length > 0)
        {
            CoverImageHandler.Instance.SetImageFromData(newMap.CoverImageData);
        }
        else CoverImageHandler.Instance.ClearImage();

        BeatmapManager.Difficulties = newMap.Difficulties;
        BeatmapManager.CurrentDifficulty = BeatmapManager.GetDefaultDifficulty();

        OnMapLoaded?.Invoke();
    }


    public static async Task<BeatmapInfo> LoadInfoAsync(string directory)
    {
        string infoPath = Path.Combine(directory, "Info.dat");

        BeatmapInfo info = await JsonReader.LoadInfoAsync(infoPath);
        if(info == null) return null;

        Debug.Log($"Loaded info for {info._songAuthorName} - {info._songName}, mapped by {info._levelAuthorName}");
        return info;
    }


    public static async Task<List<Difficulty>> LoadDiffsAsync(string directory, BeatmapInfo info)
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
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "You're already loading something!");
            Debug.LogWarning("Trying to load a map while already loading!");
            return;
        }

        HotReloader.loadedMapPath = null;

        if(mapDirectory.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if(mapDirectory.Contains("beatsaver.com/maps"))
            {
                //Direct beatsaver link, should load based on ID instead
                string ID = mapDirectory.Split("/").Last();
                StartCoroutine(LoadMapIDCoroutine(ID));

                UrlArgHandler.LoadedMapID = ID;
                return;
            }

            StartCoroutine(LoadMapURLCoroutine(mapDirectory));
            UrlArgHandler.LoadedMapURL = mapDirectory;
            return;
        }

        const string IDchars = "0123456789abcdef";
        //If the directory doesn't contain any characters that aren't hexadecimal, that means it's probably an ID
        if(!mapDirectory.Any(x => !IDchars.Contains(x)))
        {
            StartCoroutine(LoadMapIDCoroutine(mapDirectory));
            UrlArgHandler.LoadedMapID = mapDirectory;
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        //Loading files from string directories doesn't work in WebGL
        ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Invalid URL!");
#else
        UrlArgHandler.LoadedMapURL = null;

        if(mapDirectory.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            if(!File.Exists(mapDirectory))
            {
                ErrorHandler.Instance.ShowPopup(ErrorType.Error, "That file doesn't exist!");
                Debug.LogWarning("Trying to load a map from a file that doesn't exist!");
                return;
            }

            LoadMapZip(mapDirectory);
            HotReloader.loadedMapPath = mapDirectory;
            return;
        }

        if(mapDirectory.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
        {
            //User is trying to load an unzipped map, get the parent directory
            DirectoryInfo parentDir = Directory.GetParent(mapDirectory);
            mapDirectory = parentDir.FullName;
        }

        if(!Directory.Exists(mapDirectory))
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "That directory doesn't exist!");
            Debug.LogWarning("Trying to load a map from a directory that doesn't exist!");
            return;
        }

        StartCoroutine(LoadMapDirectoryCoroutine(mapDirectory));
        HotReloader.loadedMapPath = mapDirectory;
#endif
    }
}


public class LoadedMap
{
#if !UNITY_WEBGL || UNITY_EDITOR
    public LoadedMap(BeatmapInfo info, List<Difficulty> difficulties, byte[] coverImageData, AudioClip song)
#else
    public LoadedMap(BeatmapInfo info, List<Difficulty> difficulties, byte[] coverImageData, WebAudioClip song)
#endif
    {
        Info = info;
        Difficulties = difficulties;
        CoverImageData = coverImageData;
        Song = song;
    }

    public BeatmapInfo Info { get; private set; }
    public List<Difficulty> Difficulties { get; private set; }
    public byte[] CoverImageData { get; private set; }
#if !UNITY_WEBGL || UNITY_EDITOR
    public AudioClip Song { get; private set; }
#else
    public WebAudioClip Song { get; private set; }
#endif


    public static readonly LoadedMap Empty = new LoadedMap(null, new List<Difficulty>(), null, null);
}