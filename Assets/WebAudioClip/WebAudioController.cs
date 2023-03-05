using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class WebAudioController : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void Initcontroller(float volume);

    [DllImport("__Internal")]
    public static extern void CreateClip(int id, int channels, int length, int frequency);

    [DllImport("__Internal")]
    public static extern void DisposeClip(int id);
    
    [DllImport("__Internal")]
    public static extern void UploadData(int id, byte[] data, int dataLength, int frequency, string gameObjectName, string methodName);

    [DllImport("__Internal")]
    public static extern float GetSoundTime(int id);

    [DllImport("__Internal")]
    public static extern void SetVolume(float volume);

    [DllImport("__Internal")]
    public static extern void SetPlaybackSpeed(int id, float speed);
    
    [DllImport("__Internal")]
    private static extern void Start(int id, float time);

    [DllImport("__Internal")]
    private static extern void Stop(int id);

    private static WebAudioController instance;
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
            .AddComponent<WebAudioController>();
        
        // This just keeps the inspector sane
        instance.gameObject.hideFlags = HideFlags.HideAndDontSave;
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!initialized)
        {
            Initcontroller(SettingsManager.GetFloat("musicvolume"));
            initialized = true;
        }
#endif
    }


    public static AudioSource AllocateAudioSource()
    {
#if UNITY_EDITOR || !UNITY_WEBGL
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


    public static int NewClip(int channels, int length, int frequency)
    {
        // Register on JS side
        int id = clipId;
        clipId++;

        CreateClip(id, channels, length, frequency);
        return id;
    }


    public static void SetDataClip(int id, byte[] data, int frequency, Action<int> callbackMethod = null)
    {
        callback = callbackMethod;
        UploadData(id, data, data.Length, frequency, instance.name, "AudioDataCallback");
    }


    public void AudioDataCallback(int response)
    {
        if(callback != null)
        {
            callback(response);
            callback = null;
        }
    }


    public static void StartClip(int id, float time)
    {
        Start(id, time);
    }


    public static void StopClip(int id)
    {
        Stop(id);
    }
}
