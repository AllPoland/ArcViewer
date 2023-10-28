using System.IO;
using UnityEngine;

public class LogLocationHandler : MonoBehaviour
{
#if !UNITY_WEBGL || UNITY_EDITOR
    public const string LogFileName = "Player.log";

    public static string LogPath { get; private set; }


    private void Awake()
    {
#if UNITY_STANDALONE_LINUX
        //Mostly hardcoded linux log path
        LogPath = $"~/.config/unity3d/{Application.companyName}/{Application.productName}/{LogFileName}";
#elif UNITY_STANDALONE_OSX
        //MacOS log path
        LogPath = $"~/Library/Logs/{Application.companyName}/{Application.productName}/{LogFileName}";
#else
        //Logs are sent to the persistent data path by default on windows
        LogPath = Path.Combine(Application.persistentDataPath, LogFileName);
#endif
    }

// if !UNITY_WEBGL || UNITY_EDITOR
#endif
}