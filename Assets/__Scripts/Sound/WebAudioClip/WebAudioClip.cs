using System;

public class WebAudioClip : IDisposable
{
    public bool IsPlaying => isPlaying;

    public float Length => WebAudioController.GetSoundLength(clipId);

    private int clipId;
    private bool isPlaying = false;

    public float Time => WebAudioController.GetSoundTime(clipId);

    public WebAudioClip()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        throw new InvalidOperationException("WebAudioClip should only be used in WEBGL!");
#else

        // Make sure the audio controller was created
        WebAudioController.Init();

        // Creates clip in the browser via JS
        clipId = WebAudioController.NewClip();
#endif
    }


    public void Dispose()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Stop();
#endif
        WebAudioController.DisposeClip(clipId);
    }


#if UNITY_WEBGL && !UNITY_EDITOR
    public void SetData(byte[] data, bool isOgg, Action<int> callback)
    {
        WebAudioController.SetDataClip(clipId, data, isOgg, callback);
    }


    public void SetOffset(float offset)
    {
        WebAudioController.SetOffset(clipId, offset);
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