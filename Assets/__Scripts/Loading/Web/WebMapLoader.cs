using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class WebMapLoader : MonoBehaviour
{
    public static WebClient Client;
    public static int Progress;
    public static Int64 DownloadSize;

    private static byte[] result;

    public static async Task<Stream> LoadMapURL(string url)
    {
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
        Progress = 0;
        result = null;

        Client = new WebClient();

        //Get the file size prior to starting the download
        Stream sr = Client.OpenRead(url);
        DownloadSize = Convert.ToInt64(Client.ResponseHeaders["Content-Length"]);
        sr.Dispose();

        Client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(UpdateDownloadProgress);
        Client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(CompleteDownload);
        
        Client.DownloadDataAsync(new Uri(url));

        while(result == null)
        {
            await Task.Delay(10);
        }

        if(result.Length == 0)
        {
            //Download failed
            return null;
        }

        MemoryStream stream = new MemoryStream(result);
        result = null;
        
        return stream;
    }


    public static void CancelDownload()
    {
        if(Client != null && Client.IsBusy)
        {
            Client.CancelAsync();

            ErrorHandler.Instance?.DisplayPopup(ErrorType.Notification, "The download was cancelled!");
            Debug.Log("Download task cancelled!");
        }
    }


    public static void UpdateDownloadProgress(object sender, DownloadProgressChangedEventArgs args)
    {
        Progress = args.ProgressPercentage;
    }


    public static void CompleteDownload(object sender, DownloadDataCompletedEventArgs args)
    {
        result = new Byte[0];

        if(args.Error != null)
        {
            ErrorHandler.Instance?.DisplayPopup(ErrorType.Error, $"Download failed! {args.Error.Message}");
            Debug.Log($"Download task failed with error:{args.Error.Message}, {args.Error.StackTrace}");
        }
        else if(!args.Cancelled)
        {
            result = args.Result;
        }

        Client.Dispose();
        Client = null;
    }
}