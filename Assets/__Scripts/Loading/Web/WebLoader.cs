using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class WebLoader : MonoBehaviour
{
    public static long DownloadSize;

    public static UnityWebRequest uwr;

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

        // url = "https://cors-anywhere.herokuapp.com/" + url;

        try
        {
            //Get the download size before starting the download properly
            uwr = UnityWebRequest.Head(url);

            uwr.SendWebRequest();
            while(!uwr.isDone) await Task.Yield();

            if(uwr.result == UnityWebRequest.Result.Success)
            {
                DownloadSize = Convert.ToInt64(uwr.GetResponseHeader("Content-Length"));
            }
            else
            {
                Debug.LogWarning($"{uwr.error}");
                DownloadSize = 0;
            }

            //Download request
            uwr = UnityWebRequest.Get(url);

            Debug.Log("Starting download.");
            uwr.SendWebRequest();

            while(!uwr.isDone)
            {
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