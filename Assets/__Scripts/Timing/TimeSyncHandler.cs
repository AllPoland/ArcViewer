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
    private const float maxTimeDiscrepancy = 0.05f;


    public static void CheckSync()
    {
        if(!TimeManager.Playing)
        {
            return;
        }

        float musicTime = SongManager.GetSongTime();
        float discrepancy = TimeManager.CurrentTime - musicTime;

        if(Mathf.Abs(discrepancy) >= maxTimeDiscrepancy)
        {
            //Immediately snap back in sync if we're way off
            TimeManager.CurrentTime = musicTime;
            discrepancy = 0f;
        }

        //Warp the map time scale slightly to keep it on track with the music
        float timeWarp = discrepancy * timeWarpMult;
        TimeManager.TimeScale = TimeScale - (timeWarp * TimeScale);
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