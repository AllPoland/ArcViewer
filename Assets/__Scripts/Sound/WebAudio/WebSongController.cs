using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class WebSongController : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void InitSongController(float volume);

    [DllImport("__Internal")]
    public static extern void CreateSongClip();

    [DllImport("__Internal")]
    public static extern void DisposeSongClip();
    
    [DllImport("__Internal")]
    public static extern void UploadSongData(byte[] data, int dataLength, bool isOgg, string gameObjectName, string methodName);

    [DllImport("__Internal")]
    public static extern void SetSongOffset(float offset);

    [DllImport("__Internal")]
    public static extern float GetSongTime();

    [DllImport("__Internal")]
    public static extern float GetSongLength();

    [DllImport("__Internal")]
    public static extern void SetSongVolume(float volume);

    [DllImport("__Internal")]
    public static extern void SetSongPlaybackSpeed(float speed);
    
    [DllImport("__Internal")]
    public static extern void StartSong(float time);

    [DllImport("__Internal")]
    public static extern void StopSong();

    private static WebSongController instance;
    private static Action<int> callback;

#if UNITY_WEBGL && !UNITY_EDITOR
    private static bool initialized = false;
#endif


    public static void Init()
    {
        // If an instance exists, don't create others
        if (instance != null) return;

        instance = new GameObject("Web Audio Controller")
            .AddComponent<WebSongController>();

        // This just keeps the inspector sane
        instance.gameObject.hideFlags = HideFlags.HideAndDontSave;
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!initialized)
        {
            InitSongController(SettingsManager.GetFloat("musicvolume"));
            initialized = true;
        }
#endif
    }


    public static AudioSource AllocateAudioSource()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        var audioSource = instance.gameObject.AddComponent<AudioSource>();
        return audioSource;
#else
        throw new Exception("AllocateAudioSource is only available outside WebGL.");
#endif
    }


    public static void FreeAudioSource(AudioSource source)
    {
        Destroy(source);
    }


    public static void SetDataClip(byte[] data, bool isOgg, Action<int> callbackMethod = null)
    {
        callback = callbackMethod;
        UploadSongData(data, data.Length, isOgg, instance.name, "AudioDataCallback");
    }


    public void AudioDataCallback(int response)
    {
        if(callback != null)
        {
            callback(response);
            callback = null;
        }
    }
}
