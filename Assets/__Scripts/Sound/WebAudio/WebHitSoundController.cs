using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class WebHitSoundController : MonoBehaviour
{
    [DllImport("__Internal")]
    public static extern void SetHitSoundVolume(float volume);

    [DllImport("__Internal")]
    public static extern void SetChainSoundVolume(float volume);

    [DllImport("__Internal")]
    public static extern void SetHitSound(int hitSound);

    [DllImport("__Internal")]
    public static extern void SetBadHitSound(int badHitSound);

    [DllImport("__Internal")]
    public static extern void InitHitSoundController();

    [DllImport("__Internal")]
    public static extern void ScheduleHitSound(int id, float songTime, float songPlaybackSpeed);

    [DllImport("__Internal")]
    public static extern void RemakeHitSound(int id);

    [DllImport("__Internal")]
    public static extern void DisposeHitSound(int id);

    [DllImport("__Internal")]
    public static extern int AddHitSound(int id, bool badCut, bool chainLink, float playTime, float pitch);

    [DllImport("__Internal")]
    public static extern float GetHitSoundTime(int id);

    [DllImport("__Internal")]
    public static extern bool IsHitSoundBadCut(int id);

    private static WebHitSoundController instance;
    private static HashSet<int> soundIDs = new HashSet<int>();
    private static int lowestOpenID = 0;


    public static void Init()
    {
        // If an instance exists, don't create others
        if (instance != null) return;

        instance = new GameObject("Web Hit Sound Controller")
            .AddComponent<WebHitSoundController>();

        // This just keeps the inspector sane
        instance.gameObject.hideFlags = HideFlags.HideAndDontSave;

        InitHitSoundController();
    }


    private static int GetNextOpenID()
    {
        int i = lowestOpenID;
        while(soundIDs.Contains(i))
        {
            i++;
        }
        return i;
    }


    public static void CreateHitSound(bool badCut, bool chainLink, float playTime, float pitch)
    {
        foreach(int id in soundIDs)
        {
            if(ObjectManager.CheckSameTime(GetHitSoundTime(id), playTime) && IsHitSoundBadCut(id) == badCut)
            {
                //Don't schedule stacked hitsounds
                return;
            }
        }

        int newID = lowestOpenID;
        Debug.Log("Scheduling id " + newID);

        AddHitSound(newID, badCut, chainLink, playTime, pitch);
        ScheduleHitSound(newID, SongManager.GetSongTime(), TimeSyncHandler.TimeScale);

        soundIDs.Add(newID);
        lowestOpenID = GetNextOpenID();
    }


    public static void RescheduleHitsounds()
    {
        int[] idArray = soundIDs.ToArray();
        float songTime = SongManager.GetSongTime();
        foreach(int id in idArray)
        {
            Debug.Log("Rescheduling id " + id);
            RemakeHitSound(id);
            ScheduleHitSound(id, songTime, TimeSyncHandler.TimeScale);
        }
    }


    public static void ClearScheduledSounds()
    {
        Debug.Log("Clearing hitsounds");
        int[] idArray = soundIDs.ToArray();
        foreach(int id in idArray)
        {
            DisposeHitSound(id);
        }

        soundIDs.Clear();
        lowestOpenID = 0;
    }


    public void DeleteHitSound(int id)
    {
        DisposeHitSound(id);
        soundIDs.Remove(id);
        if(id < lowestOpenID)
        {
            lowestOpenID = id;
        }
    }
}