using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine.Networking;
#endif

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
    public static event Action OnReplayMapPrompt;

    //Cancels in-flight loading tasks whenever a load finishes or is cancelled,
    //so stale tasks can't clobber newer loads
    private CancellationTokenSource loadCancelSource;


    private CancellationToken BeginLoading(string message = null)
    {
        Loading = true;
        if(message != null)
        {
            LoadingMessage = message;
        }

        if(loadCancelSource == null || loadCancelSource.IsCancellationRequested)
        {
            loadCancelSource?.Dispose();
            loadCancelSource = new CancellationTokenSource();
        }
        return loadCancelSource.Token;
    }


    private void CancelPendingLoads()
    {
        loadCancelSource?.Cancel();
    }


    private async Task LoadMapDataAsync(IMapDataLoader loader, CancellationToken token)
    {
        Loading = true;

        LoadedMap mapData;
        try
        {
            mapData = await loader.GetMap();
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Map loading failed with error: {err.Message}, {err.StackTrace}");
            mapData = LoadedMap.Empty;
        }
        finally
        {
            loader.Dispose();
        }

        if(token.IsCancellationRequested)
        {
            return;
        }

        Debug.Log("Loading complete.");
        LoadingMessage = "Done";

        LoadingMessage = "Initializing";
        //Wait 2 frames to ensure the text updates
        await Awaitable.NextFrameAsync();
        await Awaitable.NextFrameAsync();

        if(!token.IsCancellationRequested)
        {
            SetMap(mapData);
        }
    }


    private async Task LoadMapZipAsync(string directory, CancellationToken token)
    {
        Loading = true;

        ZipReader zipReader = new ZipReader();
        try
        {
            Debug.Log("Loading map zip.");
            LoadingMessage = "Loading map zip";

            zipReader.Archive = ZipFile.OpenRead(directory);
        }
        catch(Exception err)
        {
            zipReader.Dispose();

            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Failed to load zip file!");
            Debug.LogWarning($"Unhandled exception loading zip: {err.Message}, {err.StackTrace}.");

            SetMap(LoadedMap.Empty);
            return;
        }

        await LoadMapDataAsync(zipReader, token);
    }


    private void LoadMapZip(string directory)
    {
        _ = LoadMapZipAsync(directory, BeginLoading());
    }


#if UNITY_WEBGL && !UNITY_EDITOR
    private async Task LoadMapZipWebGLAsync(string directory, CancellationToken token)
    {
        LoadingMessage = "Loading zip";

        Debug.Log("Starting web request.");
        using UnityWebRequest uwr = UnityWebRequest.Get(directory);
        uwr.SendWebRequest();
        while(!uwr.isDone) await Task.Yield();

        if(token.IsCancellationRequested)
        {
            return;
        }

        if(uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning(uwr.error);
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, $"Failed to load map! {uwr.error}");

            SetMap(LoadedMap.Empty);
            return;
        }

        ZipReader zipReader = new ZipReader();
        try
        {
            zipReader.ArchiveStream = new MemoryStream(uwr.downloadHandler.data);
            zipReader.Archive = new ZipArchive(zipReader.ArchiveStream, ZipArchiveMode.Read);
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to read map data with error: {e.Message}, {e.StackTrace}");
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, $"Failed to read map data!");

            zipReader.Dispose();
            SetMap(LoadedMap.Empty);
            return;
        }

        await LoadMapDataAsync(zipReader, token);
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

        _ = LoadMapZipWebGLAsync(directory, BeginLoading());
        UrlArgHandler.LoadedMapURL = null;
    }
#endif


    //Loads a map from the given prepared map task, produced by MapDownloader
    //When applySharingInfo is set, sharing parameters get updated to match the loaded map
    //Returns false when no map could be prepared, so callers can fall back or fail their own way
    private async Task<bool> TryLoadPreparedMapAsync(Task<PreparedMapLoad> mapTask, bool applySharingInfo, CancellationToken token)
    {
        LoadingMessage = "Downloading map";

        PreparedMapLoad preparedMap = null;
        try
        {
            preparedMap = await mapTask;
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to prepare map with error: {err}");
        }

        if(token.IsCancellationRequested)
        {
            return true;
        }

        if(preparedMap == null || (string.IsNullOrEmpty(preparedMap.CachedPath) && preparedMap.Stream == null))
        {
            return false;
        }

        if(applySharingInfo)
        {
            UrlArgHandler.ignoreMapForSharing = preparedMap.IgnoreMapForSharing;
            if(!string.IsNullOrEmpty(preparedMap.MapID))
            {
                UrlArgHandler.LoadedMapID = preparedMap.MapID;
            }
            else if(!string.IsNullOrEmpty(preparedMap.URL))
            {
                UrlArgHandler.LoadedMapURL = preparedMap.URL;
            }
        }

        if(!string.IsNullOrEmpty(preparedMap.CachedPath))
        {
            Debug.Log("Found map in cache.");
            await LoadMapZipAsync(preparedMap.CachedPath, token);
            return true;
        }

        if(preparedMap.Stream.CanSeek)
        {
            preparedMap.Stream.Position = 0;
        }

        ZipReader zipReader = new ZipReader(null, preparedMap.Stream);
        try
        {
            zipReader.Archive = new ZipArchive(zipReader.ArchiveStream, ZipArchiveMode.Read);
        }
        catch(Exception err)
        {
            zipReader.Dispose();

            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Failed to read map zip!");
            Debug.LogWarning($"Unhandled exception loading prepared map: {err.Message}, {err.StackTrace}");

            SetMap(LoadedMap.Empty);
            return true;
        }

        await LoadMapDataAsync(zipReader, token);
        return true;
    }


    public async void LoadMapURL(string url, string mapID = null, string mapHash = null, bool noProxy = false)
    {
        CancellationToken token = BeginLoading();
        if(!await TryLoadPreparedMapAsync(MapDownloader.PrepareMapURLAsync(url, mapID, mapHash, noProxy, false), false, token))
        {
            SetMap(LoadedMap.Empty);
        }
    }


    public async void LoadMapID(string mapID, string mapHash = null)
    {
        CancellationToken token = BeginLoading("Fetching map from BeatSaver");
        if(!await TryLoadPreparedMapAsync(MapDownloader.PrepareMapIDAsync(mapID, mapHash, false), false, token))
        {
            SetMap(LoadedMap.Empty);
        }
    }


    private async Task LoadMapFromReplayAsync(Replay loadedReplay, bool noProxy, CancellationToken token)
    {
        string mapHash = null;
        if(!string.IsNullOrEmpty(loadedReplay.info?.hash) && loadedReplay.info.hash.Length >= 40)
        {
            //For some reason replay hash fields might have extra text past the hash
            mapHash = loadedReplay.info.hash[..40];
        }

        if(mapHash == null)
        {
            Debug.Log("Invalid hash! Showing manual map selection.");

            ShowReplayMapPrompt();
            return;
        }

        Debug.Log($"Searching for map matching replay hash: {mapHash}");
        LoadingMessage = "Fetching map from BeatSaver";

        if(!await TryLoadPreparedMapAsync(MapDownloader.PrepareMapHashAsync(mapHash, noProxy), true, token))
        {
            ShowReplayMapPrompt();
        }
    }


    private void ShowReplayMapPrompt()
    {
        Debug.Log("No map download succeeded! Showing manual map selection.");
        Loading = false;
        LoadingMessage = "";

        OnReplayMapPrompt?.Invoke();
    }


    private async Task SetReplayAsync(
        Replay replay, string mapURL, string mapID, bool noProxy, Task<PreparedMapLoad> mapTask, CancellationToken token)
    {
        ReplaySourceInfo sourceInfo = ReplayManager.SourceInfo;

        sourceInfo?.ApplyTo(replay);

        //Default to beatleader source for replays loaded without an API flow
        if(sourceInfo == null)
        {
            sourceInfo = ReplaySources.BeatLeader.CreateInfo();
            ReplayManager.SourceInfo = sourceInfo;
        }

        ReplayManager.SetReplay(replay);

        Task sourceDataTask = LoadSourceDataAsync(sourceInfo, replay);
        if(mapTask != null)
        {
            if(await TryLoadPreparedMapAsync(mapTask, true, token))
            {
                return;
            }

            //The source couldn't provide a map preemptively, fall back to the usual search below
            Debug.Log("No prepared map from the replay source! Searching from replay info instead.");
        }

        string mapHash = replay.info.hash;
        if(!string.IsNullOrEmpty(mapHash) && mapHash.Length > 40)
        {
            mapHash = mapHash[..40];
        }

        if(!string.IsNullOrEmpty(mapID))
        {
            Debug.Log($"Loading map from preset ID: {mapID}");
            UrlArgHandler.LoadedMapID = mapID;
            LoadingMessage = "Fetching map from BeatSaver";
            if(!await TryLoadPreparedMapAsync(MapDownloader.PrepareMapIDAsync(mapID, mapHash, noProxy), false, token))
            {
                SetMap(LoadedMap.Empty);
            }
        }
        else if(!string.IsNullOrEmpty(mapURL))
        {
            Debug.Log($"Loading map from preset URL: {mapURL}");
            UrlArgHandler.LoadedMapURL = mapURL;
            if(!await TryLoadPreparedMapAsync(MapDownloader.PrepareMapURLAsync(mapURL, mapID, mapHash, noProxy, false), false, token))
            {
                SetMap(LoadedMap.Empty);
            }
        }
        else if(string.IsNullOrEmpty(replay.info?.hash) || replay.info.hash.Length < 40)
        {
            if(!sourceInfo.HasFallbackMap)
            {
                LoadingMessage = "Loading player profile";
                try
                {
                    await sourceDataTask;
                }
                catch(Exception err)
                {
                    Debug.LogWarning($"Replay source data loading failed with error: {err.Message}, {err.StackTrace}");
                }

                if(token.IsCancellationRequested)
                {
                    return;
                }
            }

            if(sourceInfo.HasFallbackMap)
            {
                if(!string.IsNullOrEmpty(sourceInfo.FallbackMapID))
                {
                    UrlArgHandler.LoadedMapID = sourceInfo.FallbackMapID;
                }
                else UrlArgHandler.LoadedMapURL = sourceInfo.FallbackMapDownloadURL;

                Task<PreparedMapLoad> fallbackTask = MapDownloader.PrepareMapURLAsync(
                    sourceInfo.FallbackMapDownloadURL, sourceInfo.FallbackMapID, mapHash, noProxy, false);
                if(!await TryLoadPreparedMapAsync(fallbackTask, false, token))
                {
                    SetMap(LoadedMap.Empty);
                }
            }
            else await LoadMapFromReplayAsync(replay, noProxy, token);
        }
        else await LoadMapFromReplayAsync(replay, noProxy, token);
    }


    private static Task LoadSourceDataAsync(ReplaySourceInfo sourceInfo, Replay replay)
    {
        try
        {
            return sourceInfo.LoadSourceData?.Invoke(replay) ?? Task.CompletedTask;
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Replay source data loading failed with error: {err.Message}, {err.StackTrace}");
            return Task.CompletedTask;
        }
    }


#if !UNITY_WEBGL || UNITY_EDITOR
    private async Task LoadReplayDirectoryAsync(string directory, string mapURL, CancellationToken token)
    {
        Loading = true;

        Debug.Log($"Loading replay from directory: {directory}");
        LoadingMessage = "Loading replay";

        Replay replay = null;
        try
        {
            replay = await Task.Run(() => ReplayLoader.ReplayFromDirectory(directory));
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to load replay with error: {err.Message}, {err.StackTrace}");
        }

        if(token.IsCancellationRequested)
        {
            return;
        }

        if(replay == null)
        {
            SetMap(LoadedMap.Empty);
            return;
        }

        await SetReplayAsync(replay, mapURL, null, false, null, token);
    }
#else


    private async Task LoadReplayDirectoryWebGLAsync(string directory, CancellationToken token)
    {
        LoadingMessage = "Loading replay";

        Debug.Log("Starting web request.");
        using UnityWebRequest uwr = UnityWebRequest.Get(directory);
        uwr.SendWebRequest();
        while(!uwr.isDone) await Task.Yield();

        if(token.IsCancellationRequested)
        {
            return;
        }

        if(uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning(uwr.error);
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, $"Failed to load replay! {uwr.error}");

            SetMap(LoadedMap.Empty);
            return;
        }

        Replay replay = await ReplayLoader.ReplayFromStream(new MemoryStream(uwr.downloadHandler.data));
        if(token.IsCancellationRequested)
        {
            return;
        }

        if(replay == null)
        {
            Debug.LogWarning($"Failed to read replay data!");
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, $"Failed to read replay data!");

            SetMap(LoadedMap.Empty);
            return;
        }

        await SetReplayAsync(replay, null, null, false, null, token);
    }


    public void LoadReplayDirectoryWebGL(string directory)
    {
        if(DialogueHandler.DialogueActive)
        {
            return;
        }

        if(Loading)
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "You're already loading something!");
            Debug.LogWarning("Trying to load a replay while already loading!");
            return;
        }

        ResetPendingReplay();
        _ = LoadReplayDirectoryWebGLAsync(directory, BeginLoading());
        UrlArgHandler.LoadedReplayURL = null;
    }
#endif


    private async Task LoadReplayURLAsync(
        string url, string id, string mapURL, string mapID, bool noProxy, Task<PreparedMapLoad> mapTask, CancellationToken token)
    {
        Debug.Log($"Searching for replay from: {url}");

#if !UNITY_WEBGL || UNITY_EDITOR
        CachedFile cachedFile = CacheManager.GetCachedReplay(url);
        if(!string.IsNullOrEmpty(cachedFile?.FilePath))
        {
            Debug.Log("Found replay in cache.");
            await LoadReplayDirectoryAsync(cachedFile.FilePath, cachedFile.ExtraData, token);
            return;
        }
#endif

        LoadingMessage = "Downloading replay";

        Stream replayStream = await WebLoader.LoadFileURL(url, noProxy);
        if(token.IsCancellationRequested)
        {
            replayStream?.Dispose();
            return;
        }

        if(replayStream == null)
        {
            Debug.LogWarning("Downloaded replay is null!");

            SetMap(LoadedMap.Empty);
            return;
        }

        using(replayStream)
        {
            Replay replay = await ReplayLoader.ReplayFromStream(replayStream);
            if(token.IsCancellationRequested)
            {
                return;
            }

            if(replay == null)
            {
                Debug.LogWarning("Failed to decode replay!");
                ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Failed to decode the replay!");
                SetMap(LoadedMap.Empty);
                return;
            }

#if !UNITY_WEBGL || UNITY_EDITOR
            CacheManager.SaveReplayToCache(replayStream, url, id, mapURL);
#endif

            await SetReplayAsync(replay, mapURL, mapID, noProxy, mapTask, token);
        }
    }


    //Clears any replay that's still waiting on the manual map prompt,
    //so a new replay load doesn't mix states with the pending one
    private static void ResetPendingReplay()
    {
        if(ReplayManager.IsReplayMode)
        {
            ReplayManager.Reset();
        }
    }


    private static void SetLoadedScoreID(ReplaySource source, string id)
    {
        if(source.SourceType == ReplaySourceType.ScoreSaber)
        {
            UrlArgHandler.LoadedSSScoreId = id;
        }
        else UrlArgHandler.LoadedBLReplayID = id;
    }


    public async void LoadReplayURL(string url, string id = null, string mapURL = null, string mapID = null, bool noProxy = false)
    {
        ResetPendingReplay();
        CancellationToken token = BeginLoading();
        await LoadReplayURLAsync(url, id, mapURL, mapID, noProxy, null, token);
    }


    public async void LoadReplayFromScore(ReplaySource source, string id, string mapURL = null, string mapID = null, bool noProxy = false)
    {
        ResetPendingReplay();
        CancellationToken token = BeginLoading();
        Debug.Log($"Searching for replay from {source.Name} score ID: {id}");

#if !UNITY_WEBGL || UNITY_EDITOR
        if(source.SourceType == ReplaySourceType.BeatLeader)
        {
            CachedFile cachedFile = CacheManager.GetCachedReplay(null, id);
            if(!string.IsNullOrEmpty(cachedFile?.FilePath))
            {
                Debug.Log("Found replay in cache.");
                await LoadReplayDirectoryAsync(cachedFile.FilePath, cachedFile.ExtraData, token);
                return;
            }
        }
#endif

        LoadingMessage = $"Fetching replay from {source.Name}";

        ResolvedScore resolved = null;
        try
        {
            resolved = await source.ResolveScoreAsync(id, mapURL, mapID);
        }
        catch(Exception err)
        {
            Debug.LogWarning($"{source.Name} score lookup failed with error: {err.Message}, {err.StackTrace}");
        }

        if(token.IsCancellationRequested)
        {
            return;
        }

        if(resolved == null || string.IsNullOrEmpty(resolved.ReplayURL))
        {
            Debug.Log($"Empty or nonexistent {source.Name} replay URL!");
            SetMap(LoadedMap.Empty);
            return;
        }

        if(resolved.SourceInfo != null)
        {
            ReplayManager.SourceInfo = resolved.SourceInfo;
        }

        SetLoadedScoreID(source, id);

        string replayID = source.SourceType == ReplaySourceType.BeatLeader ? id : null;
        Task<PreparedMapLoad> mapTask = MapDownloader.PrepareMapLoadAsync(resolved, noProxy);
        await LoadReplayURLAsync(resolved.ReplayURL, replayID, resolved.MapURL, resolved.MapID, noProxy, mapTask, token);
    }


    public async void LoadReplayScoreAuto(string id, string mapURL = null, string mapID = null, bool noProxy = false)
    {
        ResetPendingReplay();
        CancellationToken token = BeginLoading();
        Debug.Log($"Searching for replay from score ID: {id}");

#if !UNITY_WEBGL || UNITY_EDITOR
        CachedFile cachedFile = CacheManager.GetCachedReplay(null, id);
        if(!string.IsNullOrEmpty(cachedFile?.FilePath))
        {
            Debug.Log("Found replay in cache.");
            UrlArgHandler.LoadedBLReplayID = id;
            await LoadReplayDirectoryAsync(cachedFile.FilePath, cachedFile.ExtraData, token);
            return;
        }
#endif

        LoadingMessage = "Fetching replay";

        ReplaySource source = null;
        ResolvedScore resolved = null;
        foreach(ReplaySource candidate in ReplaySources.All)
        {
            try
            {
                resolved = await candidate.ResolveScoreAsync(id, mapURL, mapID, false);
            }
            catch(Exception err)
            {
                Debug.LogWarning($"{candidate.Name} score lookup failed with error: {err.Message}, {err.StackTrace}");
                resolved = null;
            }

            if(token.IsCancellationRequested)
            {
                return;
            }

            if(resolved != null)
            {
                source = candidate;
                break;
            }
        }

        if(resolved == null || string.IsNullOrEmpty(resolved.ReplayURL))
        {
            Debug.Log($"Empty or nonexistent replay URL for score ID: {id}");
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, $"Couldn't find a replay for score {id}!");
            SetMap(LoadedMap.Empty);
            return;
        }

        if(resolved.SourceInfo != null)
        {
            ReplayManager.SourceInfo = resolved.SourceInfo;
        }

        SetLoadedScoreID(source, id);

        string replayID = source.SourceType == ReplaySourceType.BeatLeader ? id : null;
        Task<PreparedMapLoad> mapTask = MapDownloader.PrepareMapLoadAsync(resolved, noProxy);
        await LoadReplayURLAsync(resolved.ReplayURL, replayID, resolved.MapURL, resolved.MapID, noProxy, mapTask, token);
    }


    public void HandleNewBsorV1Stream(Replay streamedReplay)
    {
        CancellationToken token = BeginLoading();
        UIStateManager.CurrentState = UIState.MapSelection;

        ReplayManager.Reset();
        _ = SetReplayAsync(streamedReplay, null, null, false, null, token);
    }


    public void StartBsorV1StreamURI(Uri baseUrl)
    {
        ResetPendingReplay();

        // We need to replace the url with the proper websocket connection
        string[] args = baseUrl.Query.TrimStart('?').Split('&');

        int playerID = -1;
        foreach(string arg in args)
        {
            if(arg.StartsWith("playerId"))
            {
                string idString = arg.TrimStart("playerId=");
                int.TryParse(idString, out playerID);
                break;
            }
        }

        if(playerID < 0)
        {
            Debug.LogWarning("No valid player ID argument in stream URL!");
            CancelMapLoading();
            return;
        }

        Uri uri = new Uri($"wss://sockets.api.beatleader.com/stream/player/listen/");

        Debug.Log($"Starting bsorV1 stream from uri: {uri}");
        LoadingMessage = "Connecting";
        Loading = true;

        BsorV1Stream stream = new BsorV1Stream(uri, playerID, HandleNewBsorV1Stream);
        ReplaySourceHandler.SetStream(stream);
    }


    private void SetMap(LoadedMap newMap)
    {
        CancelPendingLoads();
        LoadingMessage = "";
        Loading = false;

        if(newMap.Info == null || newMap.Difficulties.Count == 0 || newMap.Song == null)
        {
            Debug.LogWarning("Failed to load map file.");

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
        SongManager.Instance.MusicClip = newMap.Song;

        if(newMap.CoverImageData != null && newMap.CoverImageData.Length > 0)
        {
            CoverImageHandler.Instance.SetImageFromData(newMap.CoverImageData);
        }
        else CoverImageHandler.Instance.ClearImage();

        BeatmapManager.SetDifficulties(newMap.Difficulties);
        BeatmapManager.CurrentDifficulty = BeatmapManager.GetDefaultDifficulty();

        OnMapLoaded?.Invoke();
    }


    public void LoadMapDirectory(string directory)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        throw new InvalidOperationException("Loading from directory doesn't work on WebGL!");
#else

        if(File.Exists(directory))
        {
            if(directory.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
            {
                LoadMapZip(directory);
                HotReloader.loadedMapPath = directory;
                return;
            }

            if(directory.EndsWith(".bsor", StringComparison.InvariantCultureIgnoreCase))
            {
                ResetPendingReplay();
                _ = LoadReplayDirectoryAsync(directory, null, BeginLoading());
                return;
            }

            if(directory.EndsWith(".dat", StringComparison.InvariantCultureIgnoreCase))
            {
                if(ScoreSaberDecoder.IsScoreSaberFile(directory))
                {
                    ResetPendingReplay();
                    _ = LoadReplayDirectoryAsync(directory, null, BeginLoading());
                    return;
                }

                //User is trying to load an unzipped map, get the parent directory
                DirectoryInfo parentDir = Directory.GetParent(directory);
                FileReader fileReader = new FileReader(parentDir.FullName);
                _ = LoadMapDataAsync(fileReader, BeginLoading());
                HotReloader.loadedMapPath = parentDir.FullName;
            }
        }
        else if(Directory.Exists(directory))
        {
            FileReader fileReader = new FileReader(directory);
            _ = LoadMapDataAsync(fileReader, BeginLoading());
            HotReloader.loadedMapPath = directory;
        }
        else
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "That file or directory doesn't exist!");
            Debug.LogWarning($"Trying to load a map from a file that doesn't exist!");
        }
#endif
    }


    public void LoadMapInput(string input)
    {
        if(DialogueHandler.DialogueActive)
        {
            Debug.LogWarning("Trying to load a map while in a dialogue!");
            return;
        }

        if(Loading)
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "You're already loading something!");
            Debug.LogWarning("Trying to load a map while already loading!");
            return;
        }

        if(UIStateManager.CurrentState != UIState.MapSelection)
        {
            UIStateManager.CurrentState = UIState.MapSelection;
        }

        if(!ReplayManager.IsReplayMode)
        {
            HotReloader.loadedMapPath = null;
            UrlArgHandler.LoadedBLReplayID = null;
        }
        UrlArgHandler.ignoreMapForSharing = false;

        string decodedURL = System.Web.HttpUtility.UrlDecode(input);
        if(decodedURL.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase) || input.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase))
        {
            Uri uri = new Uri(decodedURL);
            string noQuery = uri.GetLeftPart(UriPartial.Path);

            if(noQuery.Contains("beatsaver.com/maps"))
            {
                //Direct beatsaver link, should load based on ID instead
                string ID = noQuery.Split("/").Last();
                LoadMapID(ID);

                UrlArgHandler.LoadedMapID = ID;
                return;
            }

            if(noQuery.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
            {
                LoadMapURL(decodedURL);
                UrlArgHandler.LoadedMapURL = decodedURL;
                return;
            }

            if(noQuery.EndsWith(".bsor", StringComparison.InvariantCultureIgnoreCase))
            {
                LoadReplayURL(decodedURL);
                UrlArgHandler.LoadedReplayURL = decodedURL;
                return;
            }

            if(noQuery.Contains("stream.beatleader.com", StringComparison.InvariantCultureIgnoreCase))
            {
                StartBsorV1StreamURI(uri);
                return;
            }

            Debug.LogWarning($"{decodedURL} doesn't link to a valid map!");
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Invalid URL!");
            return;
        }

        if(SettingsManager.GetBool("replaymode")
            && ReplaySources.TryParsePrefixedScoreID(input, out ReplaySource source, out string scoreID))
        {
            //Prefixed score IDs are unambiguous, so they always load a new replay,
            //even when another replay is waiting on the map prompt
            LoadReplayFromScore(source, scoreID);

            SetLoadedScoreID(source, scoreID);
            return;
        }

        if(!ReplayManager.IsReplayMode && SettingsManager.GetBool("replaymode"))
        {
            if(!input.Any(x => !char.IsDigit(x)))
            {
                LoadReplayScoreAuto(input);
                return;
            }
        }
        else
        {
            const string IDchars = "0123456789abcdef";
            //If the directory doesn't contain any characters that aren't hexadecimal, that means it's probably an ID
            if(!input.ToLower().Any(x => !IDchars.Contains(x)))
            {
                LoadMapID(input);
                UrlArgHandler.LoadedMapID = input;
                return;
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        //Loading files from string directories doesn't work in WebGL
        ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Invalid URL!");
#else
        UrlArgHandler.LoadedMapURL = null;
        LoadMapDirectory(input);
#endif
    }


    public void CancelMapLoading()
    {
        SetMap(LoadedMap.Empty);
    }


    private struct ScheduledDifficulty
    {
        //Just a container for concurrent difficulty loading
        public DifficultyBeatmap Beatmap;
        public DifficultyCharacteristic Characteristic;
        public byte[] diffData;
    }
}


public class LoadedMap
{
#if !UNITY_WEBGL || UNITY_EDITOR
    public LoadedMap(LoadedMapData mapData, byte[] coverImageData, AudioClip song)
#else
    public LoadedMap(LoadedMapData mapData, byte[] coverImageData, WebSongClip song)
#endif
    {
        MapData = mapData;
        CoverImageData = coverImageData;
        Song = song;
    }

    public LoadedMapData MapData { get; private set; }
    public BeatmapInfo Info => MapData.Info;
    public List<Difficulty> Difficulties => MapData.Difficulties;
    public byte[] CoverImageData { get; private set; }
#if !UNITY_WEBGL || UNITY_EDITOR
    public AudioClip Song { get; private set; }
#else
    public WebSongClip Song { get; private set; }
#endif

    public static LoadedMap Empty => new LoadedMap(LoadedMapData.Empty, null, null);
}


public class LoadedMapData
{
    public LoadedMapData(BeatmapInfo info)
    {
        Info = info;
        Difficulties = new List<Difficulty>();

        BpmEvents = null;
        Lightshows = null;
    }

    public BeatmapInfo Info;
    public List<Difficulty> Difficulties;

    public BeatmapBpmEvent[] BpmEvents;
    public Dictionary<string, BeatmapLightshowV4> Lightshows;

    public static LoadedMapData Empty => new LoadedMapData(null);


    public BeatmapLightshowV4 GetLightshow(string lightshowFilename)
    {
        if(Lightshows == null)
        {
            return new BeatmapLightshowV4();
        }

        if(Lightshows.TryGetValue(lightshowFilename, out BeatmapLightshowV4 lightshow))
        {
            return lightshow;
        }
        else return new BeatmapLightshowV4();
    }
}


public interface IMapDataLoader : IDisposable
{
    public Task<LoadedMap> GetMap();
    public Task<LoadedMapData> GetMapData();
}


public interface IReplayLoader : IMapDataLoader
{
    public Task<Replay> GetReplay();
}
