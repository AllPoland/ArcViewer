using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class WebMapLoader : MonoBehaviour
{
    public static int Progress;
    private static byte[] result;

    public static async Task<Stream> LoadMapURL(string url)
    {
        MemoryStream stream = null;

        if(!url.EndsWith(".zip"))
        {
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

        using(WebClient client = new WebClient())
        {
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(UpdateDownloadProgress);
            client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(CompleteDownload);
            client.DownloadDataAsync(new Uri(url));
        }

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


    public static void UpdateDownloadProgress(object sender, DownloadProgressChangedEventArgs args)
    {
        Progress = args.ProgressPercentage;
    }


    public static void CompleteDownload(object sender, DownloadDataCompletedEventArgs args)
    {
        if(args.Error != null)
        {
            Debug.Log($"Download task failed with error:{args.Error.Message}, {args.Error.StackTrace}");
            result = new byte[0];
            return;
        }

        if(args.Cancelled)
        {
            Debug.LogWarning("Download task cancelled!");
            result = new byte[0];
            return;
        }

        result = args.Result;
    }
}