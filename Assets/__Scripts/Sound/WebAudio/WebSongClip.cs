using System;

public class WebSongClip : IDisposable
{
    public bool IsPlaying => isPlaying;

    public float Length => WebSongController.GetSongLength();

    private bool isPlaying = false;

    public float Time => WebSongController.GetSongTime();

    public WebSongClip()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        throw new InvalidOperationException("WebSongClip should only be used in WEBGL!");
#else

        // Make sure the audio controller was created
        WebSongController.Init();
#endif
    }


    public void Dispose()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Stop();
#endif
        WebSongController.StopSong();
        WebSongController.DisposeSongClip();
    }


#if UNITY_WEBGL && !UNITY_EDITOR
    public void SetData(byte[] data, bool isOgg, Action<int> callback)
    {
        WebSongController.SetDataClip(data, isOgg, callback);
    }


    public void SetOffset(float offset)
    {
        WebSongController.SetSongOffset(offset);
    }


    public void SetSpeed(float speed)
    {
        WebSongController.SetSongPlaybackSpeed(speed);
    }


    public void Play(float time = 0f)
    {
        if(isPlaying) return;

        WebSongController.StartSong(time);
        isPlaying = true;
    }


    public void Stop()
    {
        if(!isPlaying) return;

        WebSongController.StopSong();
        isPlaying = false;
    }
#endif
}