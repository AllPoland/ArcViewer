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
            //Find the last event that does - if any, and apply color based on that
            LightEvent lightEvent = eventArgs.lightEvent;
            if(!useThisEvent)
            {
                lightEvent = GetLastEvent(eventArgs, id);
            }
            LightEvent nextEvent = useNextEvent ? eventArgs.nextEvent : GetNextEvent(eventArgs, id);

            Color eventColor = LightManager.GetEventColor(lightEvent, nextEvent);
            SetProperties(eventArgs.sender.GetLaserColor(eventColor), eventArgs.sender.GetLaserGlowColor(eventColor));
        }
    }


    private LightEvent GetNextEvent(LightingPropertyEventArgs eventArgs, int id)
    {
        List<LightEvent> lightEvents = eventArgs.eventList;
        int startIndex = Mathf.Clamp(eventArgs.eventIndex, 0, lightEvents.Count - 1);

        if(startIndex == nextCheckIndex)
        {
            return nextEventIndex >= 0 ? lightEvents[nextEventIndex] : null;
        }

        //Avoid looping ahead past where we know there is no event
        int maxIndex = lightEvents.Count - 1;
        if(startIndex < nextCheckIndex)
        {
            //We've moved backward, so don't loop past the previous frame's next event
            maxIndex = nextEventIndex >= 0 ? nextEventIndex : nextCheckIndex;
        }
        else if(startIndex <= nextEventIndex)
        {
            //We've moved forward, but we haven't moved past the next event yet
            nextCheckIndex = startIndex;
            return lightEvents[nextEventIndex];
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


    private LightEvent GetLastEvent(LightingPropertyEventArgs eventArgs, int id)
    {
        List<LightEvent> lightEvents = eventArgs.eventList;
        int startIndex = Mathf.Clamp(eventArgs.eventIndex, 0, lightEvents.Count - 1);

        if(startIndex == lastCheckIndex)
        {
            return lastEventIndex >= 0 ? lightEvents[lastEventIndex] : null;
        }

        //Avoid looping back past where we know there is no event
        int minIndex = 0;
        if(startIndex > lastCheckIndex)
        {
            //We've moved forward, so don't loop past the previous frame's last event
            minIndex = lastEventIndex >= 0 ? lastEventIndex : Mathf.Max(lastCheckIndex, 0);
        }
        else if(lastEventIndex >= 0 && startIndex >= lastEventIndex)
        {
            //We've moved backward, but we haven't moved past the last event
            lastCheckIndex = startIndex;
            return lightEvents[lastEventIndex];
        }
        lastCheckIndex = startIndex;

        for(int i = startIndex; i >= minIndex; i--)
        {
            if(lightEvents[i].AffectsID(id))
            {
                lastEventIndex = i;
                return lightEvents[i];
            }
        }
        lastEventIndex = -1;
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