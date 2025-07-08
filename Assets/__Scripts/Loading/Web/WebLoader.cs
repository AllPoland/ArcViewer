using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable CS4014 //Suppress warnings about lack of await for uwr.SendWebRequest()
public class WebLoader
{
    public const string CorsProxy = "https://cors.bsmg.dev/";

    //Domains listed in this array will bypass the CORS proxy
    //Map sources that include CORS headers should be added here for faster downloads
    public static readonly string[] WhitelistURLs = new string[]
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

    public static ulong DownloadSize;
    public static UnityWebRequest uwr;


    public static string GetCorsURL(string url)
    {
        if(WhitelistURLs.Any(x => url.StartsWith(x)))
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


    public static async Task<MemoryStream> StreamFromURL(string url, bool noProxy, bool sendError = true)
    {
        MapLoader.Progress = 0;
        DownloadSize = 0;

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

        try
        {
            //Download request
            uwr = UnityWebRequest.Get(url);

            Debug.Log("Starting download.");
            uwr.SendWebRequest();

            while(!uwr.isDone)
            {
                if(DownloadSize == 0)
                {
                    //GetRequestHeader returns the file size in a string,
                    //or null if the headers haven't been receieved yet
                    string sizeHeader = uwr.GetResponseHeader("Content-Length");

                    ulong outValue;
                    DownloadSize = ulong.TryParse(sizeHeader, out outValue) ? outValue : 0;
                }

                MapLoader.Progress = uwr.downloadProgress;

                await Task.Yield();
            }

            if(uwr.result != UnityWebRequest.Result.Success)
            {
                if(uwr.error == "Request aborted")
                {
                    Debug.Log("Download cancelled.");
                    if(sendError)
                    {
                        ErrorHandler.Instance.QueuePopup(ErrorType.Notification, "Download cancelled!");
                    }
                }
                else
                {
                    Debug.LogWarning($"{uwr.error}");
                    if(sendError)
                    {
                        ErrorHandler.Instance.QueuePopup(ErrorType.Error, $"Download failed! {uwr.error}");
                    }
                }

                return null;
            }
            else
            {
                return new MemoryStream(uwr.downloadHandler.data);
            }
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Download failed with exception: {e.Message}, {e.StackTrace}");
        }
        finally
        {
            if(uwr != null)
            {
                uwr.Dispose();
                uwr = null;
            }
            MapLoader.Progress = 0;
        }
        
        return null;
    }


    public static void CancelDownload()
    {
        if(uwr != null && !uwr.isDone)
        {
            uwr.Abort();
        }
    }
}