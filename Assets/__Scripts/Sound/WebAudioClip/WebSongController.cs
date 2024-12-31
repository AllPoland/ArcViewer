using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class WebSongController : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void InitSongController(float volume);

    [DllImport("__Internal")]
    public static extern void CreateSongClip(int id);

    [DllImport("__Internal")]
    public static extern void DisposeSongClip(int id);
    
    [DllImport("__Internal")]
    public static extern void UploadSongData(int id, byte[] data, int dataLength, bool isOgg, string gameObjectName, string methodName);

    [DllImport("__Internal")]
    public static extern void SetSongOffset(int id, float offset);

    [DllImport("__Internal")]
    public static extern float GetSongTime(int id);

    [DllImport("__Internal")]
    public static extern float GetSongLength(int id);

    [DllImport("__Internal")]
    public static extern void SetSongVolume(float volume);

    [DllImport("__Internal")]
    public static extern void SetSongPlaybackSpeed(int id, float speed);
    
    [DllImport("__Internal")]
    public static extern void StartSong(int id, float time);

    [DllImport("__Internal")]
    public static extern void StopSong(int id);

    private static WebSongController instance;
    private static int clipId = 0;
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


    public static int NewClip()
    {
        // Register on JS side
        int id = clipId;
        clipId++;

        CreateSongClip(id);
        return id;
    }


    public static void SetDataClip(int id, byte[] data, bool isOgg, Action<int> callbackMethod = null)
    {
        callback = callbackMethod;
        UploadSongData(id, data, data.Length, isOgg, instance.name, "AudioDataCallback");
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
