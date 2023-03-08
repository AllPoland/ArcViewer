using System;

public class WebAudioClip : IDisposable
{
    public bool IsPlaying => isPlaying;
    public float Length {get; private set;} = 0;

    private int frequency;
    private int sampleCount;
    private int channelCount;
    private int clipId;
    private bool isPlaying = false;

    public float Time => WebAudioController.GetSoundTime(clipId);

    public WebAudioClip(int lengthSamples, int channels, int sampleRate)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        throw new InvalidOperationException("WebAudioClip should only be used in WEBGL!");
#else

        // Make sure the audio controller was created
        WebAudioController.Init();

        frequency = sampleRate;
        sampleCount = lengthSamples;
        channelCount = channels;

        // Creates clip in the browser via JS
        clipId = WebAudioController.NewClip(channels, lengthSamples, frequency);
        Length = (float)lengthSamples / sampleRate;
#endif
    }


    public void Dispose()
    {
        WebAudioController.DisposeClip(clipId);
    }


#if UNITY_WEBGL
    public void SetData(byte[] data, int frequency, Action<int> callback)
    {
        WebAudioController.SetDataClip(clipId, data, frequency, callback);
    }


    public void SetOffset(float offset)
    {
        WebAudioController.SetOffset(clipId, offset);
        Length = ((float)sampleCount / frequency) - offset;
    }


    public void SetSpeed(float speed)
    {
        WebAudioController.SetPlaybackSpeed(clipId, speed);
    }


    public void Play(float time = 0f)
    {
        if(isPlaying) return;

        WebAudioController.Start(clipId, time);
        isPlaying = true;
    }


    public void Stop()
    {
        if(!isPlaying) return;

        WebAudioController.Stop(clipId);
        isPlaying = false;
    }
#endif
}