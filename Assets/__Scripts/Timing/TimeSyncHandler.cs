using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeSyncHandler : MonoBehaviour
{
    [SerializeField] private float timeWarpMult;
    [SerializeField] private float forceAlignTime;


    public void CheckSync()
    {
        if(!TimeManager.Playing) return;

        float musicTime = AudioManager.GetSongTime();
        float mapTime = AudioManager.GetMapTime();

        float discrepancy = mapTime - musicTime;
        float absDiscrepancy = Mathf.Abs(discrepancy);

        if(absDiscrepancy >= forceAlignTime)
        {
            //Immediately snap back in sync if we're way off
            //Debug.Log($"Force correcting by {discrepancy}");
            TimeManager.CurrentTime = musicTime;
            return;
        }

        //Warp the map time scale slightly to keep it on track
        float timeWarp = discrepancy * timeWarpMult;
        TimeManager.TimeScale = 1 - timeWarp;
    }


    private void Update()
    {
        CheckSync();
    }
}