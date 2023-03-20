using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class WebLoader : MonoBehaviour
{
    public const string CorsProxy = "https://cors.bsmg.dev/";

    //Domains listed in this array will bypass the CORS proxy
    //Map sources that include CORS headers should be added here for faster downloads
    public static readonly string[] WhitelistURLs = new string[]
    {
        "https://r2cdn.beatsaver.com",
        "https://cdn.beatsaver.com"
    };

    public static ulong DownloadSize;
    public static UnityWebRequest uwr;


    public static string GetCorsURL(string url)
    {
        if(WhitelistURLs.Any(x => url.StartsWith(x)))
        {
            //The requested domain has CORS headers, so no need for a proxy
            return url;
        }

        return CorsProxy + url;
    }


    public static async Task<Stream> LoadMapURL(string url)
    {
        await Task.Yield();

        MemoryStream stream = null;

        if(!url.EndsWith(".zip"))
        {
            ErrorHandler.Instance?.QueuePopup(ErrorType.Error, "The url doesn't link to a zip!");
            Debug.Log("Attempted to load a map from a non-zip url.");
            return stream;
        }

        stream = await StreamFromURL(url);

        return stream;
    }


    public static async Task<MemoryStream> StreamFromURL(string url)
    {
        MapLoader.Progress = 0;
        DownloadSize = 0;

#if UNITY_WEBGL && !UNITY_EDITOR
        url = GetCorsURL(url);
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
                    ErrorHandler.Instance.QueuePopup(ErrorType.Notification, "Download cancelled!");
                }
                else
                {
                    Debug.LogWarning($"{uwr.error}");
                    ErrorHandler.Instance.QueuePopup(ErrorType.Error, $"Download failed! {uwr.error}");
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
            Debug.LogWarning($"Map download failed with exception: {e.Message}, {e.StackTrace}");
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