using System.Collections.Generic;
using UnityEngine;

public class LightHandler : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshRenderer glowRenderer;

    [Header("Parameters")]
    [SerializeField] public LightEventType type;
    [SerializeField] public int id;
    [SerializeField] public float emissionMult = 1f;
    [SerializeField] public float glowMult = 1f;

    private MaterialPropertyBlock laserProperties;
    private MaterialPropertyBlock glowProperties;

    private int lastCheckIndex = -1;
    private int nextCheckIndex = -1;

    private int lastEventIndex = -1;
    private int nextEventIndex = -1;


    private void UpdateLight(LightingPropertyEventArgs eventArgs)
    {
        if(eventArgs.type != type)
        {
            return;
        }

        bool useThisEvent = eventArgs.lightEvent?.AffectsID(id) ?? true;
        bool useNextEvent = eventArgs.nextEvent?.AffectsID(id) ?? true;
        if(useThisEvent && useNextEvent)
        {
            //The current event and next events affect this id, so no need to recalculate anything
            //This will always be the case in vanilla lightshows without lightID
            SetProperties(eventArgs.laserColor, eventArgs.glowColor);
        }
        else
        {
            //Either the current event or the next don't affect this ID
            LightEvent lightEvent;
            LightEvent nextEvent;
            if(useThisEvent)
            {
                //Use the default lightEvent
                lightEvent = eventArgs.lightEvent;
                //Find the next event that affects this ID
                nextEvent = GetNextEvent(eventArgs, eventArgs.eventIndex);
            }
            else
            {
                //Find the last event that affects this ID
                int eventIndex = GetLastEventIndex(eventArgs);
                lightEvent = eventIndex >= 0 ? eventArgs.eventList[eventIndex] : null;
                //Find the next event based on the current event's position
                //(eventIndex is clamped inside the method)
                nextEvent = GetNextEvent(eventArgs, eventIndex);
            }

            Color eventColor = LightManager.GetEventColor(lightEvent, nextEvent);
            SetProperties(eventArgs.sender.GetLaserColor(eventColor), eventArgs.sender.GetLaserGlowColor(eventColor));
        }
    }


    private int GetLastEventIndex(LightingPropertyEventArgs eventArgs)
    {
        List<LightEvent> lightEvents = eventArgs.eventList;
        int startIndex = Mathf.Clamp(eventArgs.eventIndex - 1, 0, lightEvents.Count - 1);

        if(startIndex == lastCheckIndex)
        {
            return lastEventIndex;
        }

        if(startIndex < lastCheckIndex)
        {
            if(lastCheckIndex >= 0 && lastEventIndex < 0)
            {
                //We've already looked behind and found no last event
                return -1;
            }
            else if(lastEventIndex >= 0 && startIndex > lastEventIndex)
            {
                //We've moved backward, but we haven't moved past the last event
                return lastEventIndex;
            }
        }

        //Avoid looping back past where we know there is no event
        int minIndex = 0;
        if(startIndex > lastCheckIndex)
        {
            //We've moved forward, so don't loop past the previous frame's last event
            minIndex = lastEventIndex >= 0 ? lastEventIndex : Mathf.Max(lastCheckIndex, 0);
        }
        lastCheckIndex = startIndex;

        for(int i = startIndex; i >= minIndex; i--)
        {
            if(lightEvents[i].AffectsID(id))
            {
                lastEventIndex = i;
                return i;
            }
        }
        lastEventIndex = -1;
        return -1;
    }


    private LightEvent GetNextEvent(LightingPropertyEventArgs eventArgs, int eventIndex)
    {
        List<LightEvent> lightEvents = eventArgs.eventList;
        int startIndex = Mathf.Clamp(eventIndex + 1, 0, lightEvents.Count - 1);

        if(startIndex == nextCheckIndex)
        {
            return nextEventIndex >= 0 ? lightEvents[nextEventIndex] : null;
        }

        if(startIndex > nextCheckIndex)
        {
            if(nextCheckIndex >= 0 && nextEventIndex < 0)
            {
                //We've already looked ahead and found no next event
                return null;
            }
            else if(startIndex < nextEventIndex)
            {
                //We've moved forward, but we haven't moved past the next event yet
                return lightEvents[nextEventIndex];
            }
        }

        //Avoid looping ahead past where we know there is no event
        int maxIndex = lightEvents.Count - 1;
        if(startIndex < nextCheckIndex)
        {
            //We've moved backward, so don't loop past the previous frame's next event
            maxIndex = nextEventIndex >= 0 ? nextEventIndex : nextCheckIndex;
        }
        nextCheckIndex = startIndex;

        for(int i = startIndex; i <= maxIndex; i++)
        {
            if(lightEvents[i].AffectsID(id))
            {
                nextEventIndex = i;
                return lightEvents[i];
            }
        }
        nextEventIndex = -1;
        return null;
    }


    private void ResetCheckIndices()
    {
        lastCheckIndex = -1;
        nextCheckIndex = -1;

        lastEventIndex = -1;
        nextEventIndex = -1;
    }


    private void UpdateDifficulty(Difficulty difficulty)
    {
        ResetCheckIndices();
    }


    private void SetProperties(Color laserColor, Color glowColor)
    {
        laserProperties.SetColor("_LaserColor", laserColor);
        glowProperties.SetColor("_LaserColor", glowColor);

        UpdateProperties();
    }


    private void UpdateProperties()
    {
        meshRenderer.SetPropertyBlock(laserProperties);
        glowRenderer.SetPropertyBlock(glowProperties);
    }


    private void Awake()
    {
        laserProperties = new MaterialPropertyBlock();
        glowProperties = new MaterialPropertyBlock();

        laserProperties.SetFloat("_ColorMult", emissionMult);
        glowProperties.SetFloat("_ColorMult", glowMult);
    }


    private void OnEnable()
    {
        LightManager.OnLightPropertiesChanged += UpdateLight;
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateDifficulty;
    }


    private void OnDisable()
    {
        LightManager.OnLightPropertiesChanged -= UpdateLight;
        BeatmapManager.OnBeatmapDifficultyChanged -= UpdateDifficulty;
    }
}