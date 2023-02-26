using System;
using UnityEngine;

public class WebAudioClip : IDisposable
{
    public bool IsPlaying => isPlaying;
    public float Length {get; private set;} = 0;

    private int frequency;
    private int channelCount;
    private int clipId;
    private bool isPlaying = false;

#if UNITY_EDITOR
    private AudioSource unitySource;
    private AudioClip unityClip;

    public float Time => (float)(unitySource.timeSamples) / unityClip.frequency;
#else
    public float Time => WebAudioController.GetSoundTime(clipId);
#endif

    public WebAudioClip(int lengthSamples, int channels, int sampleRate)
    {
        // Make sure the audio controller was created
        WebAudioController.Init();

        frequency = sampleRate;
        channelCount = channels;

#if UNITY_EDITOR
        unitySource = WebAudioController.AllocateAudioSource();
        unityClip = AudioClip.Create("TMP", lengthSamples, channels, frequency, false);
        unitySource.clip = unityClip;
#else
        // Creates clip in the browser via JS
        clipId = WebAudioController.NewClip(channels, lengthSamples, frequency);
#endif

        Length = lengthSamples / sampleRate;
    }


    public void Dispose()
    {
#if UNITY_EDITOR
        WebAudioController.FreeAudioSource(unitySource);

        if(unityClip != null)
        {
            unityClip.UnloadAudioData();
            GameObject.Destroy(unityClip);
        }
#else
        // Clean up memory
        WebAudioController.DisposeClip(clipId);
#endif
    }


    public void SetData(float[] data, int offsetSamples = 0)
    {
#if UNITY_EDITOR
        unityClip.SetData(data, offsetSamples);
#else
        WebAudioController.SetDataClip(clipId, data, offsetSamples, channelCount, frequency);
#endif
    }


    public void SetSpeed(float speed)
    {
#if UNITY_EDITOR
        if(unitySource != null)
        {
            unitySource.pitch = speed;
        }
#else
        WebAudioController.SetPlaybackSpeed(clipId, speed);
#endif
    }


    public void Play(float time = 0f)
    {
        if(isPlaying) return;
#if UNITY_EDITOR
        unitySource.Play();
        unitySource.time = time;
#else
        WebAudioController.StartClip(clipId, time);
#endif
        isPlaying = true;
    }


    public void Stop()
    {
        if(!isPlaying) return;
#if UNITY_EDITOR
        unitySource.Stop();
#else
        WebAudioController.StopClip(clipId);
#endif
        isPlaying = false;
    }
}
