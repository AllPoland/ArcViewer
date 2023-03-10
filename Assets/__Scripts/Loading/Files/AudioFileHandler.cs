using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
// using NVorbis;

public class AudioFileHandler
{
#if UNITY_WEBGL && !UNITY_EDITOR
    private static AudioUploadState uploadState;


    public static async Task<WebAudioClip> WebAudioClipFromStream(MemoryStream stream)
    {
        WebAudioClip newClip = null;
        try
        {
            //Create the audio clip where we'll write the audio data
            newClip = new WebAudioClip();

            byte[] data = stream.ToArray();
            newClip.SetData(data, ClipUploadResultCallback);
            uploadState = AudioUploadState.uploading;

            while(uploadState == AudioUploadState.uploading)
            {
                await Task.Yield();
            }

            if(uploadState == AudioUploadState.error)
            {
                //An error occurred in the upload
                newClip.Dispose();
                return null;
            }

            return newClip;
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to load audio data with error: {e.Message}, {e.StackTrace}");

            if(newClip != null)
            {
                newClip.Dispose();
            }

            return null;
        }
    }


    public static void ClipUploadResultCallback(int result)
    {
        if(result < 1)
        {
            //A result below 1 means setting data failed
            Debug.LogWarning("Failed to upload audio data!");
            uploadState = AudioUploadState.error;
        }
        else
        {
            uploadState = AudioUploadState.success;
        }
    }


    public enum AudioUploadState
    {
        inactive,
        uploading,
        success,
        error
    }
#endif
}