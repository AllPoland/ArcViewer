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


#if !UNITY_WEBGL || UNITY_EDITOR
    private IEnumerator ReloadMapCoroutine(IMapDataLoader loader)
    {
        Loading = true;

        using Task<LoadedMapData> loadingTask = Task.Run(() => loader.GetMapData());
        yield return new WaitUntil(() => loadingTask.IsCompleted);
        LoadedMapData mapData = loadingTask.Result;

        if(mapData.Info == null || mapData.Difficulties == null || mapData.Difficulties.Count == 0)
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Failed to reload map!");
            CancelLoading();
            yield break;
        }

        loader.Dispose();
        UpdateMap(mapData);

        Loading = false;
    }


    private void ReloadMapDirectory()
    {
        if(!Directory.Exists(loadedMapPath))
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "The map folder has been deleted!");
            Debug.LogWarning("The map folder has been deleted!");
            return;
        }

        FileReader fileReader = new FileReader(loadedMapPath);
        loadingCoroutine = StartCoroutine(ReloadMapCoroutine(fileReader));
    }


    private void ReloadMapZip()
    {
        if(!File.Exists(loadedMapPath))
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "The map file has been deleted!");
            Debug.LogWarning("The map file has been deleted!");
            return;
        }
        Loading = true;

        Debug.Log("Loading zip file.");
        try
        {
            ZipArchive archive = ZipFile.OpenRead(loadedMapPath);
            ZipReader zipReader = new ZipReader(archive);

            loadingCoroutine = StartCoroutine(ReloadMapCoroutine(zipReader));
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to load map zip with error: {e.Message}, {e.StackTrace}");
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Failed to load map zip!");
            return;
        }
    }


    private void UpdateMap(LoadedMapData mapData)
    {
        Difficulty currentDiff = BeatmapManager.CurrentDifficulty;
        DifficultyCharacteristic currentCharacteristic = currentDiff.characteristic;
        DifficultyRank currentRank = currentDiff.difficultyRank;

        BeatmapManager.Info = mapData.Info;
        BeatmapManager.Difficulties = mapData.Difficulties;

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
            ErrorHandler.Instance.ShowPopup(ErrorType.Notification, "The current difficulty was deleted!");
        }
    }
#endif


    public void ReloadMap()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        throw new InvalidOperationException("Hot reloading doesn't work in WebGL!");
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
            ReloadMapZip();
        }
        else
        {
            ReloadMapDirectory();
        }
#endif
    }


    public void CancelLoading()
    {
        if(loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }
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