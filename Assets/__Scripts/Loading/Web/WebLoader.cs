using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable CS4014 //Suppress warnings about lack of await for uwr.SendWebRequest()
public class WebLoader
{
    public const string CorsProxy = "https://cors.bsmg.dev/";

    //Domains listed in this array will bypass the CORS proxy
    //Map sources that include CORS headers should be added here for faster downloads
    private static readonly string[] DefaultWhitelistURLs = new string[]
    {
        "https://r2cdn.beatsaver.com",
        "https://cdn.beatsaver.com",
        "https://api.beatleader.xyz",
        "https://cdn.replays.beatleader.xyz/",
        "https://api.beatleader.com",
        "https://cdn.replays.beatleader.com/",
        "https://cdn.songs.beatleader.xyz/",
        "https://cdn.songs.beatleader.com/"
    };

    public static string[] WhitelistURLs => DefaultWhitelistURLs
        .Concat(ReplaySources.All.SelectMany(x => x.CorsURLs))
        .Where(x => !string.IsNullOrEmpty(x))
        .Distinct()
        .ToArray();

    public static ulong DownloadSize;
    public static UnityWebRequest uwr;
    private static readonly List<UnityWebRequest> ActiveRequests = new List<UnityWebRequest>();


    public static string GetCorsURL(string url)
    {
        if(WhitelistURLs.Any(x => url.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
        {
            return url;
        }

        Debug.Log($"Downloading via CORS proxy.");
        return CorsProxy + url;
    }


    public static async Task<Stream> LoadFileURL(string url, bool noProxy, bool sendError = true)
    {
        await Task.Yield();
        return await StreamFromURL(url, noProxy, sendError);
    }


    //Aggregates progress across all concurrent downloads so they don't fight over the loading bar
    private static void UpdateDownloadProgress()
    {
        if(ActiveRequests.Count == 0)
        {
            DownloadSize = 0;
            MapLoader.Progress = 0;
            return;
        }

        ulong totalSize = 0;
        ulong totalDownloaded = 0;
        float progressSum = 0f;
        bool sizesKnown = true;

        foreach(UnityWebRequest request in ActiveRequests)
        {
            //GetResponseHeader returns the file size in a string,
            //or null if the headers haven't been receieved yet
            string sizeHeader = request.GetResponseHeader("Content-Length");
            if(ulong.TryParse(sizeHeader, out ulong size) && size > 0)
            {
                totalSize += size;
                totalDownloaded += request.downloadedBytes;
            }
            else sizesKnown = false;

            progressSum += Mathf.Max(request.downloadProgress, 0f);
        }

        DownloadSize = sizesKnown ? totalSize : 0;
        if(sizesKnown && totalSize > 0)
        {
            MapLoader.Progress = (float)totalDownloaded / totalSize;
        }
        else
        {
            //Without every download size, fall back to averaging request progress
            MapLoader.Progress = progressSum / ActiveRequests.Count;
        }
    }


    public static async Task<MemoryStream> StreamFromURL(string url, bool noProxy, bool sendError = true)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if(!noProxy)
        {
            url = GetCorsURL(url);
        }
        else
        {
            Debug.Log("CORS proxy is disabled.");
        }
#endif

        UnityWebRequest request = null;
        try
        {
            request = UnityWebRequest.Get(url);
            ActiveRequests.Add(request);
            uwr = request;

            Debug.Log("Starting download.");
            request.SendWebRequest();

            while(!request.isDone)
            {
                UpdateDownloadProgress();
                await Task.Yield();
            }

            if(request.result != UnityWebRequest.Result.Success)
            {
                if(request.error == "Request aborted")
                {
                    Debug.Log("Download cancelled.");
                    if(sendError)
                    {
                        ErrorHandler.Instance.QueuePopup(ErrorType.Notification, "Download cancelled!");
                    }
                }
                else
                {
                    Debug.LogWarning($"{request.error}");
                    if(sendError)
                    {
                        ErrorHandler.Instance.QueuePopup(ErrorType.Error, $"Download failed! {request.error}");
                    }
                }

                return null;
            }
            else
            {
                return new MemoryStream(request.downloadHandler.data);
            }
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Download failed with exception: {e.Message}, {e.StackTrace}");
        }
        finally
        {
            if(request != null)
            {
                ActiveRequests.Remove(request);
                request.Dispose();
                uwr = ActiveRequests.Count > 0 ? ActiveRequests[^1] : null;
            }
            UpdateDownloadProgress();
        }
        
        return null;
    }


    public static void CancelDownload()
    {
        foreach(UnityWebRequest request in ActiveRequests)
        {
            if(request != null && !request.isDone)
            {
                request.Abort();
            }
        }
    }
}