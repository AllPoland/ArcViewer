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
        }
    }

    public static Action<float> OnTimeScaleChanged;

    [SerializeField] private float timeWarpMult;
    [SerializeField] private float forceAlignTime;


    public void CheckSync()
    {
        if(!TimeManager.Playing) return;

        float musicTime = SongManager.GetSongTime();
        float mapTime = TimeManager.CurrentTime;

        float discrepancy = mapTime - musicTime;
        float absDiscrepancy = Mathf.Abs(discrepancy);

        if(absDiscrepancy >= forceAlignTime || TimeScale == 0)
        {
            //Immediately snap back in sync if we're way off
            //Debug.Log($"Force correcting by {discrepancy}");
            TimeManager.CurrentTime = musicTime;
        }
        
        //Warp the map time scale slightly to keep it on track
        float timeWarp = discrepancy * timeWarpMult;
        TimeManager.TimeScale = TimeScale - timeWarp;
        
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