using System;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;

public class ReplayLoader
{
//Suppress warnings about a lack of await when building for WebGL
#pragma warning disable 1998
    public static async Task<Replay> ReplayFromDirectory(string directory)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        throw new InvalidOperationException("Loading from directory doesn't work in WebGL!");
#else
        try
        {
            byte[] replayData = await File.ReadAllBytesAsync(directory);
            return ReplayDecoder.Decode(replayData);
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to load replay with error: {e.Message}, {e.StackTrace}");
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Failed to load replay file!");
            return null;
        }
#endif
    }
}