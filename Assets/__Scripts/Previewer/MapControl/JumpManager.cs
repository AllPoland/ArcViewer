using UnityEngine;

public class JumpManager : MonoBehaviour
{
    public float NJS { get; private set; }
    public float JumpDistance { get; private set; }

    public float HalfJumpDistance { get; private set; }
    public float ReactionTime { get; private set; }

    public float EffectiveNJS { get; private set; }
    public float EffectiveHalfJumpDistance { get; private set; }

    public float NjsRelativeMult => EffectiveNJS / BeatmapManager.NJS;

    private MapElementList<NjsEvent> njsEvents = new MapElementList<NjsEvent>();

    private ObjectManager objectManager => ObjectManager.Instance;
    private bool useVariableNJS => objectManager.forceGameAccuracy || SettingsManager.GetBool("variablenjs");


    public bool CheckInSpawnRange(float time, bool extendBehindCamera = false, bool includeMoveTime = true, float hitOffset = 0f)
    {
        float despawnTime = extendBehindCamera ? TimeManager.CurrentTime + objectManager.BehindCameraTime : TimeManager.CurrentTime;
        float spawnTime = TimeManager.CurrentTime + ReactionTime;
        if(includeMoveTime)
        {
            spawnTime += objectManager.moveTime;
        }

        float hitTime = extendBehindCamera ? time : time - hitOffset;
        return time <= spawnTime && hitTime > despawnTime;
    }


    public bool DurationObjectInSpawnRange(float startTime, float endTime, bool extendBehindCamera = true, bool includeMoveTime = true)
    {
        if(extendBehindCamera)
        {
            endTime -= objectManager.BehindCameraTime;
        }

        bool timeInRange = TimeManager.CurrentTime >= startTime && TimeManager.CurrentTime <= endTime;
        return timeInRange || CheckInSpawnRange(startTime, extendBehindCamera, includeMoveTime);
    }


    public float GetZPosition(float objectTime)
    {
        float reactionTime = ReactionTime;
        float jumpTime = TimeManager.CurrentTime + reactionTime;

        if(objectTime <= jumpTime)
        {
            //Note has jumped in. Place based on Jump Setting stuff
            float timeDist = objectTime - TimeManager.CurrentTime;
            return WorldSpaceFromTimeAdjusted(timeDist);
        }
        else
        {
            //Note hasn't jumped in yet. Place based on the jump-in stuff
            float timeDist = (objectTime - jumpTime) / objectManager.moveTime;
            return EffectiveHalfJumpDistance + (objectManager.moveZ * timeDist);
        }
    }


    public float WorldSpaceFromTime(float time)
    {
        return time * BeatmapManager.NJS;
    }


    public float WorldSpaceFromTimeAdjusted(float time)
    {
        return (time * EffectiveNJS) + objectManager.CutPlanePos;
    }


    public float SpawnParabola(float targetHeight, float baseHeight, float t)
    {
        float dSquared = Mathf.Pow(EffectiveHalfJumpDistance, 2);
        float tSquared = Mathf.Pow(t, 2);

        float movementRange = targetHeight - baseHeight;

        return Mathf.Clamp(-(movementRange / dSquared) * tSquared + targetHeight, -9999f, 9999f);
    }


    public float GetObjectY(float startY, float targetY, float objectTime)
    {
        float jumpTime = TimeManager.CurrentTime + ReactionTime;

        if(objectTime > jumpTime)
        {
            return startY;
        }

        return SpawnParabola(targetY, startY, GetZPosition(objectTime) - objectManager.CutPlanePos);
    }


    private void SetDefaultJump()
    {
        NJS = BeatmapManager.NJS;
        JumpDistance = BeatmapManager.JumpDistance;

        HalfJumpDistance = JumpDistance * 0.5f;
        ReactionTime = HalfJumpDistance / NJS;
    }


    private void SetNjsOffset(float njsOffset)
    {
        NJS = Mathf.Max(BeatmapManager.NJS + njsOffset, 0.01f);

        //Scale up reaction time based on the decrease in NJS
        //The result of this is that an increase in NJS keeps RT constant
        //while a decrease in NJS keeps JD constant
        float njsRatio = Mathf.Min(NJS / BeatmapManager.NJS, 1f);
        ReactionTime = Mathf.Approximately(njsRatio, 0f) ? 1000f : BeatmapManager.ReactionTime / njsRatio;
        HalfJumpDistance = ReactionTime * NJS;
        JumpDistance = HalfJumpDistance * 2f;
    }


    private void SetNjsEvents(float currentOffset, float currentTime, NjsEvent nextEvent)
    {
        if(nextEvent != null)
        {
            //Interpolate between the current event's NJS and the next event NJS
            float transitionTime = nextEvent.Time - currentTime;
            float t = (TimeManager.CurrentTime - currentTime) / transitionTime;
            t = BeatSaberEasings.Ease(t, nextEvent.Easing);

            currentOffset = Mathf.Lerp(currentOffset, nextEvent.RelativeNJS, t);
        }

        SetNjsOffset(currentOffset);
    }


    private void UpdateEffectiveNJS()
    {
        if(!ReplayManager.IsReplayMode)
        {
            EffectiveNJS = NJS;
        }

        float halfJumpDistance = HalfJumpDistance;
        float adjustedJumpDistance = halfJumpDistance - objectManager.CutPlanePos;

        float njsMult = adjustedJumpDistance / halfJumpDistance;
        EffectiveNJS = NJS * njsMult;

        if(float.IsNaN(EffectiveNJS) || Mathf.Abs(EffectiveNJS) < 0.01f)
        {
            EffectiveNJS = 0.01f;
        }

        EffectiveHalfJumpDistance = ReactionTime * EffectiveNJS;
    }


    public void UpdateNjs(float beat)
    {
        if(njsEvents.Count == 0 || !useVariableNJS)
        {
            //No NJS events, so just use the map defaults
            SetDefaultJump();
            UpdateEffectiveNJS();
            return;
        }

        int lastIndex = njsEvents.GetLastIndex(TimeManager.CurrentTime, x => x.Beat <= beat);

        bool foundEvent = lastIndex >= 0;
        NjsEvent currentEvent = foundEvent ? njsEvents[lastIndex] : null;
        float currentOffset = foundEvent ? currentEvent.RelativeNJS : 0f;
        float currentTime = foundEvent ? currentEvent.Time : 0f;

        if(foundEvent && currentEvent.Extend)
        {
            //The current event is an extension, use the previous non-extension offset
            for(int i = lastIndex - 1; i >= 0; i--)
            {
                currentEvent = njsEvents[i];
                if(!currentEvent.Extend)
                {
                    //This is the previous non-extend event, inherit its offset
                    currentOffset = currentEvent.RelativeNJS;
                    break;
                }
                else if(i == 0)
                {
                    //This is the last event, and we found no non-extension events
                    //That means the njs offset hasn't been changed by anything yet
                    currentOffset = 0f;
                }
            }
        }

        bool hasNextEvent = lastIndex + 1 < njsEvents.Count;
        NjsEvent nextEvent = hasNextEvent ? njsEvents[lastIndex + 1] : null;
        if(hasNextEvent && nextEvent.Extend)
        {
            //The next event is an extension, so ignore it for now
            nextEvent = null;
        }

        SetNjsEvents(currentOffset, currentTime, nextEvent);
        UpdateEffectiveNJS();
    }


    private void UpdateDifficulty(Difficulty newDifficulty)
    {
        njsEvents.Clear();

        foreach(BeatmapNjsEvent ne in newDifficulty.beatmapDifficulty.NjsEvents)
        {
            njsEvents.Add(new NjsEvent(ne));
        }
        njsEvents.SortElementsByBeat();

        UpdateNjs(TimeManager.CurrentBeat);
    }


    private void Start()
    {
        TimeManager.OnDifficultyBpmEventsLoaded += UpdateDifficulty;
        TimeManager.OnBeatChangedEarly += UpdateNjs;
    }
}


public class NjsEvent : MapElement
{
    public bool Extend;
    public BeatSaberEasingType Easing;
    public float RelativeNJS;


    public NjsEvent(BeatmapNjsEvent ne)
    {
        Beat = ne.b;
        Extend = ne.p > 0;
        Easing = (BeatSaberEasingType)ne.e;
        RelativeNJS = ne.d;
    }
}