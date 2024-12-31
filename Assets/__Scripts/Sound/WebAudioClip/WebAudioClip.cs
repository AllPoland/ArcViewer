using System;

public class WebSongClip : IDisposable
{
    public bool IsPlaying => isPlaying;

    public float Length => WebSongController.GetSongLength(clipId);

    private int clipId;
    private bool isPlaying = false;

    public float Time => WebSongController.GetSongTime(clipId);

    public WebSongClip()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        throw new InvalidOperationException("WebSongClip should only be used in WEBGL!");
#else

        // Make sure the audio controller was created
        WebSongController.Init();

        // Creates clip in the browser via JS
        clipId = WebSongController.NewClip();
#endif
    }


    public void Dispose()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Stop();
#endif
        WebSongController.DisposeSongClip(clipId);
    }


#if UNITY_WEBGL && !UNITY_EDITOR
    public void SetData(byte[] data, bool isOgg, Action<int> callback)
    {
        WebSongController.SetDataClip(clipId, data, isOgg, callback);
    }


    public void SetOffset(float offset)
    {
        WebSongController.SetSongOffset(clipId, offset);
    }


    public void SetSpeed(float speed)
    {
        WebSongController.SetSongPlaybackSpeed(clipId, speed);
    }


    public void Play(float time = 0f)
    {
        if(isPlaying) return;

        WebSongController.StartSong(clipId, time);
        isPlaying = true;
    }


    public void Stop()
    {
        if(!isPlaying) return;

        WebSongController.StopSong(clipId);
        isPlaying = false;
    }
#endif
}