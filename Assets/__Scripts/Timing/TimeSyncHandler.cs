using System;
using UnityEngine;

public class TimeSyncHandler : MonoBehaviour
{
    private static float _speed = 1f;
    public static float TimeScale
    {
        get => _speed;

        set
        {
            _speed = value;
            OnTimeScaleChanged?.Invoke(value);
            CheckSync();
        }
    }

    public static Action<float> OnTimeScaleChanged;

    private const float timeWarpMult = 1f;
    private const float maxTimeWarp = 0.5f;


    public static void CheckSync()
    {
        if(!TimeManager.Playing)
        {
            return;
        }

        //Warp the map time scale slightly to keep it on track with the music
        float musicTime = SongManager.GetSongTime();
        float mapTime = TimeManager.CurrentTime;

        float discrepancy = mapTime - musicTime;
        float timeWarp = discrepancy * timeWarpMult;
        TimeManager.TimeScale = TimeScale - (timeWarp * TimeScale);

        if(Mathf.Abs(1f - (TimeManager.TimeScale / TimeScale)) >= maxTimeWarp)
        {
            //Immediately snap back in sync if we're way off
            TimeManager.CurrentTime = musicTime;
            TimeManager.TimeScale = 1f;
        }  
    }


    public void UpdateState(UIState newState)
    {
        if(ReplayManager.IsReplayMode)
        {
            TimeScale = ReplayManager.ReplayTimeScale;
        }
        else TimeScale = 1f;
    }


    private void Update()
    {
        CheckSync();
    }


    private void Start()
    {
        UIStateManager.OnUIStateChanged += UpdateState;
    }
}