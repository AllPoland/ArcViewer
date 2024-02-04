using System;
using System.IO;
using UnityEngine;

public class CacheManager : MonoBehaviour
{
    public const string CacheFolder = "Cache";

    public static string CachePath;

    private const string MapCacheJson = "CacheInfo.json";
    private const string ReplayCacheJson = "ReplayCacheInfo.json";

    private static FileCache MapCache;
    private static FileCache ReplayCache;


#if !UNITY_WEBGL || UNITY_EDITOR
    public static CachedFile GetCachedMap(string url = null, string id = null, string hash = null)
    {
        if(id != null)
        {
            //First search, only allowing the latest map to pass
            CachedFile foundFile = MapCache.GetCachedFile(url, id, hash, "latest");
            if(foundFile == null)
            {
                //Search with other parameters only
                return MapCache.GetCachedFile(url, null, hash);
            }
            else return foundFile;
        }
        else return MapCache.GetCachedFile(url, id, hash);
    }


    public static void SaveMapToCache(Stream fileStream, string url = null, string id = null, string hash = null, string extraData = null)
    {
        MapCache.SaveFileToCache(fileStream, url, id, hash, extraData);
    }


    public static CachedFile GetCachedReplay(string url = null, string id = null, string hash = null)
    {
        return ReplayCache.GetCachedFile(url, id, hash);
    }


    public static void SaveReplayToCache(Stream fileStream, string url = null, string id = null, string extraData = null)
    {
        ReplayCache.SaveFileToCache(fileStream, url, id, null, extraData);
    }


    public static void ClearCache()
    {
        if(!Directory.Exists(CachePath))
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Notification, "The cache is already empty!");
            return;
        }

        try
        {
            Debug.Log($"Deleting cache directory: {CachePath}.");
            Directory.Delete(CachePath, true);

            MapCache.Clear();
            ReplayCache.Clear();
            ErrorHandler.Instance.ShowPopup(ErrorType.Notification, "Successfully cleared the cache.");
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to delete path: {CachePath} with error: {e.Message}, {e.StackTrace}");
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Failed to clear the cache!");
        }
    }


    private void UpdateSettings(string setting)
    {
        bool allSettings = setting == "all";
        if(allSettings || setting == "cachesize")
        {
            MapCache.MaxCacheSize = SettingsManager.GetInt("cachesize");
        }
        if(allSettings || setting == "replaycachesize")
        {
            ReplayCache.MaxCacheSize = SettingsManager.GetInt("replaycachesize");
        }
    }


    private void Awake()
    {
        CachePath = Path.Combine(Application.persistentDataPath, CacheFolder);

        MapCache = new FileCache(MapCacheJson, ".zip");
        ReplayCache = new FileCache(ReplayCacheJson, ".bsor");
    }


    private void Start()
    {
        if(SettingsManager.Loaded)
        {
            UpdateSettings("all");
        }
        SettingsManager.OnSettingsUpdated += UpdateSettings;
    }
#endif
}