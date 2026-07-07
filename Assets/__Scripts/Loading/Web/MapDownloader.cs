using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class PreparedMapLoad
{
    public Stream Stream;
    public string CachedPath;
    public string URL;
    public string MapID;
    public string MapHash;
    public bool IgnoreMapForSharing;
}


//Utility class handling map lookup and downloading
//This resolves score/replay/ID/hash/URL inputs down to a zip stream or cached file,
//so MapLoader only has to orchestrate the loading workflow
public static class MapDownloader
{
    public static Task<PreparedMapLoad> PrepareMapLoadAsync(ResolvedScore resolved, bool noProxy)
    {
        string mapHash = resolved.SourceInfo?.MapHash;
        if(!string.IsNullOrEmpty(resolved.MapURL))
        {
            return PrepareMapURLAsync(resolved.MapURL, resolved.MapID, mapHash, noProxy, false);
        }

        if(!string.IsNullOrEmpty(resolved.MapID))
        {
            return PrepareMapIDAsync(resolved.MapID, mapHash, noProxy);
        }

        if(!string.IsNullOrEmpty(mapHash))
        {
            return PrepareMapHashAsync(mapHash, noProxy);
        }

        if(resolved.SourceInfo?.HasFallbackMap == true)
        {
            return PrepareMapURLAsync(resolved.SourceInfo.FallbackMapDownloadURL, resolved.SourceInfo.FallbackMapID, mapHash, noProxy, false);
        }

        return Task.FromResult<PreparedMapLoad>(null);
    }


    public static async Task<PreparedMapLoad> PrepareMapIDAsync(string mapID, string mapHash, bool noProxy)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        CachedFile cachedFile = CacheManager.GetCachedMap(null, mapID, mapHash);
        if(!string.IsNullOrEmpty(cachedFile?.FilePath))
        {
            return PreparedMapFromCache(cachedFile, mapID, mapHash, false);
        }
#endif

        Debug.Log($"Getting BeatSaver response for ID: {mapID}");
        string mapURL = await BeatSaverHandler.GetBeatSaverMapID(mapID);
        if(string.IsNullOrEmpty(mapURL)) return null;

        mapURL = System.Web.HttpUtility.UrlDecode(mapURL);
        return await PrepareMapURLAsync(mapURL, mapID, mapHash, noProxy, false);
    }


    public static async Task<PreparedMapLoad> PrepareMapHashAsync(string mapHash, bool noProxy)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        CachedFile cachedFile = CacheManager.GetCachedMap(null, null, mapHash);
        if(!string.IsNullOrEmpty(cachedFile?.FilePath) && (!string.IsNullOrEmpty(cachedFile.ID) || !string.IsNullOrEmpty(cachedFile.URL)))
        {
            return PreparedMapFromCache(cachedFile, null, mapHash, true);
        }
#endif

        Debug.Log($"Getting BeatSaver response for hash: {mapHash}");
        (string[] mapURLs, string mapID) = await BeatSaverHandler.GetBeatSaverMapHash(mapHash);
        if(mapURLs == null || mapURLs.Length == 0) return null;

        for(int i = 0; i < mapURLs.Length; i++)
        {
            mapURLs[i] = System.Web.HttpUtility.UrlDecode(mapURLs[i]);
        }

        return await PrepareMapURLsAsync(mapURLs, mapID, mapHash, noProxy, true);
    }


    public static async Task<PreparedMapLoad> PrepareMapURLAsync(string url, string mapID, string mapHash, bool noProxy, bool ignoreMapForSharing)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        CachedFile cachedFile = CacheManager.GetCachedMap(url, mapID, mapHash);
        if(!string.IsNullOrEmpty(cachedFile?.FilePath))
        {
            return PreparedMapFromCache(cachedFile, mapID, mapHash, ignoreMapForSharing);
        }
#endif

        Debug.Log($"Downloading map data from: {url}");
        Stream zipStream = await WebLoader.LoadFileURL(url, noProxy);
        if(zipStream == null) return null;

#if !UNITY_WEBGL || UNITY_EDITOR
        string extraData = mapID == null ? null : "latest";
        CacheManager.SaveMapToCache(zipStream, url, mapID, mapHash, extraData);
#endif

        return new PreparedMapLoad
        {
            Stream = zipStream,
            URL = url,
            MapID = mapID,
            MapHash = mapHash,
            IgnoreMapForSharing = ignoreMapForSharing
        };
    }


    public static async Task<PreparedMapLoad> PrepareMapURLsAsync(
        string[] urls, string mapID, string mapHash, bool noProxy, bool ignoreMapForSharing)
    {
        for(int i = 0; i < urls.Length; i++)
        {
            string url = urls[i];

#if !UNITY_WEBGL || UNITY_EDITOR
            CachedFile cachedFile = CacheManager.GetCachedMap(url, mapID, mapHash);
            if(!string.IsNullOrEmpty(cachedFile?.FilePath))
            {
                return PreparedMapFromCache(cachedFile, mapID, mapHash, ignoreMapForSharing);
            }
#endif

            Debug.Log($"Downloading map data from: {url}");
            Stream zipStream = await WebLoader.LoadFileURL(url, noProxy, false);
            if(zipStream == null)
            {
                Debug.LogWarning("Downloaded data is null!");
                continue;
            }

#if !UNITY_WEBGL || UNITY_EDITOR
            CacheManager.SaveMapToCache(zipStream, url, mapID, mapHash);
#endif

            return new PreparedMapLoad
            {
                Stream = zipStream,
                URL = url,
                MapID = mapID,
                MapHash = mapHash,
                IgnoreMapForSharing = ignoreMapForSharing
            };
        }

        return null;
    }


    private static PreparedMapLoad PreparedMapFromCache(CachedFile cachedFile, string mapID, string mapHash, bool ignoreMapForSharing)
    {
        return new PreparedMapLoad
        {
            CachedPath = cachedFile.FilePath,
            URL = cachedFile.URL,
            MapID = string.IsNullOrEmpty(cachedFile.ID) ? mapID : cachedFile.ID,
            MapHash = mapHash,
            IgnoreMapForSharing = ignoreMapForSharing
        };
    }

}
