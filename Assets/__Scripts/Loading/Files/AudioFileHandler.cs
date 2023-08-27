using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Winista.Mime;

public static class AudioFileHandler
{
#if UNITY_WEBGL && !UNITY_EDITOR
    private static AudioUploadState uploadState;


    public static async Task<WebAudioClip> WebAudioClipFromStream(MemoryStream stream, string filename)
    {
        WebAudioClip newClip = null;
        try
        {
            //Create the audio clip where we'll write the audio data
            newClip = new WebAudioClip();

            byte[] data = stream.ToArray();
            bool isOgg = filename.EndsWith(".ogg", StringComparison.InvariantCultureIgnoreCase)
                || filename.EndsWith(".egg", StringComparison.InvariantCultureIgnoreCase)
                || GetAudioTypeFromData(data) == AudioType.OGGVORBIS;

            newClip.SetData(data, isOgg, ClipUploadResultCallback);
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
            newClip?.Dispose();

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
#else


    public static async Task<AudioClip> LoadAudioDirectory(string directory, string filename = "")
    {
        if(!File.Exists(directory))
        {
            return null;
        }

        string fileString = string.IsNullOrEmpty(filename) ? directory : filename;
        AudioType type = await GetAudioTypeFromFile(fileString, directory);
        Debug.Log($"Loading audio file with type of {type}.");
        return await GetAudioFromFile(directory, type);
    }


    public static async Task<AudioType> GetAudioTypeFromFile(string fileName, string directory = "")
    {
        //Uncomment to get the type based on file extension
        //(Marginal performance improvement but makes files with incorrect extension unloadable)
        // if(fileName.EndsWith(".ogg", StringComparison.InvariantCultureIgnoreCase) || fileName.EndsWith(".egg", StringComparison.InvariantCultureIgnoreCase))
        // {
        //     return AudioType.OGGVORBIS;
        // }
        // else if(fileName.EndsWith(".wav", StringComparison.InvariantCultureIgnoreCase))
        // {
        //     return AudioType.WAV;
        // }
        // else if(fileName.EndsWith(".mp3", StringComparison.InvariantCultureIgnoreCase))
        // {
        //     return AudioType.MPEG;
        // }

        if(string.IsNullOrEmpty(directory))
        {
            //If no directory is given, the name is the directory
            directory = fileName;
        }

        if(!File.Exists(directory)) return AudioType.UNKNOWN;

        try
        {
            // Debug.Log($"Unable to match audio type by file extension from {fileName}");
            byte[] audioData = await File.ReadAllBytesAsync(directory);
            return GetAudioTypeFromData(audioData);
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to read audio file data with error: {e.Message}, {e.StackTrace}");
            return AudioType.UNKNOWN;
        }
    }


    public static async Task<AudioClip> GetAudioFromFile(string path, AudioType type)
    {
        if(!File.Exists(path))
        {
            Debug.LogWarning("Trying to load audio from a nonexistant file!");
            return null;
        }

        Uri uri = new Uri("file://" + path);

        AudioClip song = null;
        try
        {
            DownloadHandlerAudioClip downloadHandler = new DownloadHandlerAudioClip(uri, type);

            using(UnityWebRequest audioUwr = UnityWebRequestMultimedia.GetAudioClip(uri, type))
            {
                Debug.Log("Getting AudioClip from file.");
                audioUwr.SendWebRequest();

                while(!audioUwr.isDone) await Task.Yield();

                if(audioUwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"{audioUwr.error}");
                    return song;
                }
                else
                {
                    song = DownloadHandlerAudioClip.GetContent(audioUwr);
                }
            }
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Audio loading failed with exception: {err.Message}, {err.StackTrace}");
        }

        return song;
    }
#endif


    public static AudioType GetAudioTypeFromData(byte[] data)
    {
        MimeTypes mimeTypes = new MimeTypes();

        MimeType type = mimeTypes.GetMimeType(data);

        Debug.Log($"Audio is mime type {type.Name}");

        switch(type.Name)
        {
            case "audio/wav":
            case "audio/x-wav":
                return AudioType.WAV;
            case "audio/ogg":
            case "application/x-ogg":
                return AudioType.OGGVORBIS;
            case "audio/mpeg":
                return AudioType.MPEG;
            default:
                return AudioType.UNKNOWN;
        }
    }
}