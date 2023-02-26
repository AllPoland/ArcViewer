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

    //Used by ZipReader since you can't get Application.persistentDataPath on a secondary thread
    public static string persistentDataPath;


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
#if UNITY_WEBGL
        Debug.LogWarning("Tried to load from directory in WebGL!");
        ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Loading from directory doesn't work in WebGL!");

        UpdateMapInfo(null, new List<Difficulty>(), null, null);
        yield break;
#else
        Loading = true;

        Debug.Log("Loading info.");
        LoadingMessage = "Loading Info.dat";
        Task<BeatmapInfo> infoTask = Task.Run(() => LoadInfoAsync(directory));

        yield return new WaitUntil(() => infoTask.IsCompleted);
        BeatmapInfo info = infoTask.Result;

        infoTask.Dispose();

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

        diffTask.Dispose();

        LoadingMessage = "Loading song";
        string audioDirectory = Path.Combine(directory, info._songFilename);
        AudioClip song = null;
        if(File.Exists(audioDirectory))
        {
            AudioType type = FileUtil.GetAudioTypeByDirectory(info._songFilename);
            Debug.Log($"Loading audio file with type of {type}.");

            Task<AudioClip> audioTask = FileUtil.GetAudioFromFile(audioDirectory, type);
            yield return new WaitUntil(() => audioTask.IsCompleted);

            song = audioTask.Result;
            audioTask.Dispose();
        }
        else
        {
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Unable to load audio file!");
            Debug.LogWarning("Song file not found!");

            UpdateMapInfo(null, new List<Difficulty>(), null, null);
            yield break;
        }

        if(song == null)
        {
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Unable to load audio file!");
            Debug.LogWarning("Song file not found!");

            UpdateMapInfo(null, new List<Difficulty>(), null, null);
            yield break;
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
#endif
    }


    private IEnumerator LoadMapZipArchiveCoroutine(ZipArchive archive, Stream zipStream = null)
    {
        Debug.Log("Loading map zip.");
        LoadingMessage = "Loading map zip";

#if !UNITY_WEBGL
        Task<(BeatmapInfo, List<Difficulty>, TempFile, byte[])> loadZipTask = Task.Run(() => ZipReader.GetMapFromZipArchiveAsync(archive));
#else
        Task<(BeatmapInfo, List<Difficulty>, Stream, byte[])> loadZipTask = ZipReader.GetMapFromZipArchiveAsync(archive);
#endif

        yield return new WaitUntil(() => loadZipTask.IsCompleted);
        var result = loadZipTask.Result;
        BeatmapInfo info = result.Item1;
        List<Difficulty> difficulties = result.Item2;
#if !UNITY_WEBGL
        TempFile songData = result.Item3;
#else
        //Webgl reads song directly from zip
        Stream songData = result.Item3;
#endif
        byte[] coverImageData = result.Item4;

        loadZipTask.Dispose();

        if(info == null)
        {
            DisposeZip();

            UpdateMapInfo(null, new List<Difficulty>(), null, null);
            yield break;
        }

        if(songData == null)
        {
            DisposeZip();

            UpdateMapInfo(null, new List<Difficulty>(), null, null);
            yield break;
        }

        LoadingMessage = "Loading song";
#if !UNITY_WEBGL
        AudioClip song = null;
        if(File.Exists(songData.Path))
        {
            AudioType type = FileUtil.GetAudioTypeByDirectory(info._songFilename);
            Debug.Log($"Loading audio file with type of {type}.");

            Task<AudioClip> audioTask = FileUtil.GetAudioFromFile(songData.Path, type);
            yield return new WaitUntil(() => audioTask.IsCompleted);

            song = audioTask.Result;
            audioTask.Dispose();
        }
        else
        {
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Unable to load audio file!");
            Debug.LogWarning("Song file not found!");

            DisposeZip();

            UpdateMapInfo(null, new List<Difficulty>(), null, null);
            yield break;
        }
#else
        Debug.Log("Loading audio file.");
        WebAudioClip song = null;

        AudioType audioType = FileUtil.GetAudioTypeByDirectory(info._songFilename);

        if(audioType == AudioType.OGGVORBIS)
        {
            Task<WebAudioClip> audioTask = AudioFileHandler.ClipFromOGGAsync(songData);
            yield return new WaitUntil(() => audioTask.IsCompleted);

            song = audioTask.Result;
            audioTask.Dispose();
        }
        else if(audioType == AudioType.WAV)
        {
            Task<WebAudioClip> audioTask = AudioFileHandler.ClipFromWavAsync(songData);
            yield return new WaitUntil(() => audioTask.IsCompleted);

            song = audioTask.Result;
            audioTask.Dispose();
        }
        else
        {
            Debug.LogWarning("Song file is in an unsupported type!");
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Song file is in an unsupported type!");

            DisposeZip();

            UpdateMapInfo(null, new List<Difficulty>(), null, null);
            yield break;
        }
#endif

        if(song == null)
        {
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Unable to load audio file!");
            Debug.LogWarning("Failed to load AudioClip!");

            DisposeZip();

            UpdateMapInfo(null, new List<Difficulty>(), null, null);
            yield break;
        }

        Debug.Log("Loading complete.");
        LoadingMessage = "Done";

        DisposeZip();

        UpdateMapInfo(info, difficulties, song, coverImageData);


        void DisposeZip()
        {
            if(zipStream != null)
            {
                zipStream.Dispose();
            }
            if(songData != null)
            {
                songData.Dispose();
            }
            if(loadZipTask != null)
            {
                loadZipTask.Dispose();
            }
            archive.Dispose();
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
            if(archive != null)
            {
                archive.Dispose();
            }

            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Failed to load zip file!");
            Debug.LogWarning($"Unhandled exception loading zip: {err.Message}, {err.StackTrace}.");

            UpdateMapInfo(null, new List<Difficulty>(), null, null);
        }
    }


#if UNITY_WEBGL && !UNITY_EDITOR
    private IEnumerator LoadMapZipWebGLCoroutine(string directory)
    {
        Loading = true;
        LoadingMessage = "Loading zip";

        Stream zipStream = null;
        ZipArchive archive = null;

        UnityWebRequest uwr = UnityWebRequest.Get(directory);
        
        Debug.Log("Starting web request.");
        yield return uwr.SendWebRequest();

        if(uwr.result == UnityWebRequest.Result.Success)
        {
            try
            {
                zipStream = new MemoryStream(uwr.downloadHandler.data);
                archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

                uwr.Dispose();

                StartCoroutine(LoadMapZipArchiveCoroutine(archive, zipStream));
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to read map data with error: {e.Message}, {e.StackTrace}");
                ErrorHandler.Instance.DisplayPopup(ErrorType.Error, $"Failed to read map data!");

                if(zipStream != null)
                {
                    zipStream.Dispose();
                }
                if(archive != null)
                {
                    archive.Dispose();
                }

                uwr?.Dispose();

                UpdateMapInfo(null, new List<Difficulty>(), null, null);
                yield break;
            }
        }
        else
        {
            Debug.LogWarning(uwr.error);
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, $"Failed to load map! {uwr.error}");

            if(zipStream != null)
            {
                zipStream.Dispose();
            }
            if(archive != null)
            {
                archive.Dispose();
            }

            uwr.Dispose();

            UpdateMapInfo(null, new List<Difficulty>(), null, null);
            yield break;
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
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "You're already loading something!");
            Debug.LogWarning("Trying to load a map while already loading!");
            return;
        }

        StartCoroutine(LoadMapZipWebGLCoroutine(directory));
    }
#endif


    public IEnumerator LoadMapURLCoroutine(string url, string mapID = null)
    {
        Loading = true;

#if !UNITY_WEBGL || UNITY_EDITOR
        //Use the map id as the map key to avoid making requests to beatsaver for cached maps
        //This method of doing this is pretty yucky but it gets the job done so fuck it
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

        Task<Stream> downloadTask = WebLoader.LoadMapURL(url);

        yield return new WaitUntil(() => downloadTask.IsCompleted);
        Stream zipStream = downloadTask.Result;

        downloadTask.Dispose();

        if(zipStream == null)
        {
            Debug.LogWarning("Downloaded data is null!");

            UpdateMapInfo(null, new List<Difficulty>(), null, null);
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
            if(archive != null)
            {
                archive.Dispose();
            }
            zipStream.Dispose();

            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Failed to read map zip!");
            Debug.LogWarning($"Unhandled exception loading zip URL: {err.Message}, {err.StackTrace}");

            UpdateMapInfo(null, new List<Difficulty>(), null, null);
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
        Task<string> apiTask = BeatSaverHandler.GetBeatSaverMapURL(mapID);

        yield return new WaitUntil(() => apiTask.IsCompleted);
        string mapURL = apiTask.Result;

        apiTask.Dispose();

        if(mapURL == null || mapURL == "")
        {
            Debug.Log("Empty or nonexistant URL!");
            UpdateMapInfo(null, new List<Difficulty>(), null, null);
            yield break;
        }

        StartCoroutine(LoadMapURLCoroutine(mapURL, mapID));
    }

#if !UNITY_WEBGL
    private void UpdateMapInfo(BeatmapInfo info, List<Difficulty> difficulties, AudioClip song, byte[] coverData)
#else
    private void UpdateMapInfo(BeatmapInfo info, List<Difficulty> difficulties, WebAudioClip song, byte[] coverData)
#endif
    {
        StopAllCoroutines();
        LoadingMessage = "";
        Loading = false;
        
        if(info == null || difficulties.Count == 0 || song == null)
        {
            Debug.LogWarning("Failed to load map file");

            if(song != null)
            {
#if!UNITY_WEBGL
                song.UnloadAudioData();
                Destroy(song);
#else
                song.Dispose();
#endif
            }
            UIStateManager.CurrentState = UIState.MapSelection;

            return;
        }

        UIStateManager.CurrentState = UIState.Previewer;
        
        BeatmapManager.Info = info;
        AudioManager.Instance.MusicClip = song;

        if(coverData != null && coverData.Length > 0)
        {
            CoverImageHandler.Instance.SetImageFromData(coverData);
        }
        else CoverImageHandler.Instance.ClearImage();

        BeatmapManager.Difficulties = difficulties;
        BeatmapManager.CurrentMap = BeatmapManager.GetDefaultDifficulty();

        OnMapLoaded?.Invoke();
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

            StartCoroutine(LoadMapURLCoroutine(mapDirectory));
            return;
        }

        const string IDchars = "0123456789abcdef";
        //If the directory doesn't contain any characters that aren't hexadecimal, that means it's probably an ID
        if(!mapDirectory.Any(x => !IDchars.Contains(x)))
        {
            StartCoroutine(LoadMapIDCoroutine(mapDirectory));
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        //Loading files from string directories doesn't work in WebGL
        ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Invalid URL!");
#else
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

        if(!Directory.Exists(mapDirectory))
        {
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "That directory doesn't exist!");
            Debug.LogWarning("Trying to load a map from a directory that doesn't exist!");
            return;
        }

        StartCoroutine(LoadMapDirectoryCoroutine(mapDirectory));
#endif
    }


    private void Awake()
    {
        persistentDataPath = Application.persistentDataPath;
    }
}