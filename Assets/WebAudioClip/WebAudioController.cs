using System.Runtime.InteropServices;
using UnityEngine;

public class WebAudioController : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void Initcontroller();

    [DllImport("__Internal")]
    public static extern void CreateClip(int id, int channels, int length, int frequency);

    [DllImport("__Internal")]
    public static extern void DisposeClip(int id);
    
    [DllImport("__Internal")]
    public static extern void UploadData(int id, float[] array, int size, int offset, int channels, int frequency);

    [DllImport("__Internal")]
    public static extern float GetSoundTime(int id);

    [DllImport("__Internal")]
    public static extern void SetVolume(float volume);
    
    [DllImport("__Internal")]
    private static extern void Start(int id, float time);

    [DllImport("__Internal")]
    private static extern void Stop(int id);

    static WebAudioController me;

    static int clipId = 0;

#if !UNITY_EDITOR
    static bool initialized = false;
#endif


    public static void Init()
    {
#if UNITY_EDITOR
        // If an instance exists, don't create others
        if (me != null) return;

        me = new GameObject("===WEB AUDIO CONTROLLER===")
            .AddComponent<WebAudioController>();
        
        // This just keeps the inspector sane
        me.gameObject.hideFlags = HideFlags.HideAndDontSave;
#else
        if (!initialized)
        {
            Initcontroller();
            initialized = true;
        }
#endif
    }


    public static AudioSource AllocateAudioSource()
    {
#if UNITY_EDITOR || !UNITY_WEBGL
        var audioSource = me.gameObject.AddComponent<AudioSource>();
        return audioSource;
#else
        throw new System.Exception("AllocateAudioSource is only available outside WebGL.");
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


    public static void SetDataClip(int id, float[] data, int offsetSamples, int channelCount, int frequency)
    {
        UploadData(id, data, data.Length, offsetSamples, channelCount, frequency);
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
