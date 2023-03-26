using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;

public class HotReloader : MonoBehaviour
{
    public static string loadedMapPath = null;

    private static bool _loading;
    public static bool Loading
    {
        get => _loading;
        
        set
        {
            _loading = value;
            OnLoadingChanged?.Invoke(value);
        }
    }
    public static event Action<bool> OnLoadingChanged;

    private Coroutine loadingCoroutine;
    private Stream mapZipStream;
    private ZipArchive mapZipArchive;


#if !UNITY_WEBGL || UNITY_EDITOR
    private IEnumerator ReloadMapDirectoryCoroutine()
    {
        if(!Directory.Exists(loadedMapPath))
        {
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "The map folder has been deleted!");
            Debug.LogWarning("The map folder has been deleted!");
            yield break;
        }
        Loading = true;

        //Reloading should only load the info and difficulties, to maximise speed
        Debug.Log("Loading Info.dat.");
        BeatmapInfo info = null;
        using(Task<BeatmapInfo> infoTask = Task.Run(() => MapLoader.LoadInfoAsync(loadedMapPath)))
        {
            yield return new WaitUntil(() => infoTask.IsCompleted);
            info = infoTask.Result;
        }

        if(info == null)
        {
            //Failed to load info
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Failed to load Info.dat!");
            Debug.LogWarning("Failed to load Info.dat!");

            CancelLoading();
            yield break;
        }

        Debug.Log("Loading difficulties.");
        List<Difficulty> difficulties = new List<Difficulty>();
        using(Task<List<Difficulty>> diffTask = Task.Run(() => MapLoader.LoadDiffsAsync(loadedMapPath, info)))
        {
            yield return new WaitUntil(() => diffTask.IsCompleted);
            difficulties = diffTask.Result;
        }

        if(difficulties.Count == 0)
        {
            //Failed to load difficulties (or the map just has none, in either case, don't replace the loaded map)
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Failed to load map difficulties!");
            Debug.LogWarning("Failed to load difficulties!");

            CancelLoading();
            yield break;
        }

        UpdateMap(info, difficulties);

        Loading = false;
    }


    private IEnumerator ReloadMapZipCoroutine()
    {
        if(!File.Exists(loadedMapPath))
        {
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "The map file has been deleted!");
            Debug.LogWarning("The map file has been deleted!");
            yield break;
        }
        Loading = true;

        Debug.Log("Loading zip file.");
        try
        {
            mapZipStream = File.OpenRead(loadedMapPath);
            mapZipArchive = new ZipArchive(mapZipStream, ZipArchiveMode.Read);
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to load map zip with error: {e.Message}, {e.StackTrace}");
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Failed to load map zip!");

            CancelLoading();
            yield break;
        }

        Debug.Log("Loading Info.dat.");
        BeatmapInfo info = null;
        using(Task<BeatmapInfo> infoTask = Task.Run(() => ZipReader.GetInfoData(mapZipArchive)))
        {
            yield return new WaitUntil(() => infoTask.IsCompleted);
            info = infoTask.Result;
        }

        if(info == null)
        {
            //Errors are logged in ZipReader.GetInfoData
            CancelLoading();
            yield break;
        }

        Debug.Log("Loading difficulties.");
        List<Difficulty> difficulties = new List<Difficulty>();
        using(Task<List<Difficulty>> diffTask = Task.Run(() =>
            ZipReader.GetDifficultiesAsync(info._difficultyBeatmapSets, mapZipArchive)))
        {
            yield return new WaitUntil(() => diffTask.IsCompleted);
            difficulties = diffTask.Result;
        }

        if(difficulties.Count == 0)
        {
            //Failed to load difficulties (or the map just has none, in either case, don't replace the loaded map)
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Failed to load map difficulties!");
            Debug.LogWarning("Failed to load difficulties!");

            CancelLoading();
            yield break;
        }

        UpdateMap(info, difficulties);
        DisposeZip();

        Loading = false;
    }


    private void UpdateMap(BeatmapInfo info, List<Difficulty> difficulties)
    {
        Difficulty currentDiff = BeatmapManager.CurrentDifficulty;
        DifficultyCharacteristic currentCharacteristic = currentDiff.characteristic;
        DifficultyRank currentRank = currentDiff.difficultyRank;

        BeatmapManager.Info = info;
        BeatmapManager.Difficulties = difficulties;

        //Set the difficulty to the one matching the current characteristic and rank
        List<Difficulty> characteristicDiffs = BeatmapManager.GetDifficultiesByCharacteristic(currentCharacteristic);
        Difficulty newDiff = characteristicDiffs.Find(x => x.difficultyRank == currentRank);

        if(newDiff != null)
        {
            BeatmapManager.CurrentDifficulty = newDiff;
        }
        else
        {
            //Unable to find a matching diff, so just use the default instead
            BeatmapManager.CurrentDifficulty = BeatmapManager.GetDefaultDifficulty();
            ErrorHandler.Instance.DisplayPopup(ErrorType.Notification, "The current difficulty was deleted!");
        }
    }
#endif


    public void ReloadMap()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        throw new InvalidOperationException("Hot reloading doesn't work in browser!");
#else
        if(Loading)
        {
            Debug.LogWarning("Tried to reload while already loading!");
            return;
        }

        if(string.IsNullOrEmpty(loadedMapPath))
        {
            Debug.LogWarning("Tried to reload with no path!");
            return;
        }

        Debug.Log($"Reloading map from {loadedMapPath}");
        if(loadedMapPath.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
        {
            loadingCoroutine = StartCoroutine(ReloadMapZipCoroutine());
        }
        else
        {
            loadingCoroutine = StartCoroutine(ReloadMapDirectoryCoroutine());
        }
#endif
    }


    private void DisposeZip()
    {
        mapZipArchive?.Dispose();
        mapZipStream?.Dispose();
    }


    public void CancelLoading()
    {
        if(!Loading)
        {
            //No need to cancel if we aren't loading
            return;
        }

        if(loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }

        DisposeZip();
        Loading = false;
    }


    public static void ClearLoadedMap()
    {
        loadedMapPath = null;
    }


    public void UpdateState(UIState newState)
    {
        if(newState != UIState.Previewer)
        {
            CancelLoading();
            ClearLoadedMap();
        }
    }


    private void start()
    {
        MapLoader.OnLoadingFailed += ClearLoadedMap;
    }


    private void OnEnable()
    {
        UIStateManager.OnUIStateChanged += UpdateState;
    }


    private void OnDisable()
    {
        CancelLoading();

        UIStateManager.OnUIStateChanged -= UpdateState;
    }
}