using UnityEngine;

public class JumpManager : MonoBehaviour
{
    public float NJS { get; private set; }
    public float JumpDistance { get; private set; }

    public float HalfJumpDistance { get; private set; }
    public float ReactionTime { get; private set; }

    private MapElementList<NjsEvent> njsEvents = new MapElementList<NjsEvent>();


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


    private void UpdateNjs(float beat)
    {
        if(njsEvents.Count == 0)
        {
            //No NJS events, so just use the map defaults
            SetDefaultJump();
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