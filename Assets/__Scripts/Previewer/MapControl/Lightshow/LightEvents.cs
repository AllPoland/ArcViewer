using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightEvent : MapElement
{
    public LightEventType Type;
    public LightEventValue Value;
    public float FloatValue = 1f;

    public Color? CustomColor;
    public int[] LightIDs;
    public Easings.EasingType TransitionEasing;
    public bool HsvLerp = false;

    public LightEvent LastGlobalEvent;

    private Dictionary<int, LightEvent> lastEvents;
    private Dictionary<int, LightEvent> nextEvents;

    public bool IsTransition => Value == LightEventValue.RedTransition || Value == LightEventValue.BlueTransition || Value == LightEventValue.WhiteTransition;


    public LightEvent() {}

    public LightEvent(BeatmapBasicBeatmapEvent beatmapEvent)
    {
        Beat = beatmapEvent.b;
        Type = (LightEventType)beatmapEvent.et;
        Value = (LightEventValue)beatmapEvent.i;
        FloatValue = beatmapEvent.f;
        TransitionEasing = Easings.EasingType.Linear;

        if(Type == LightEventType.BackLasers && LightManager.FlipBackLasers)
        {
            //Back laser events need to have red/blue values swapped in some envs
            MirrorColor();
        }

        if(beatmapEvent.customData != null)
        {
            BeatmapCustomBasicEventData customData = beatmapEvent.customData;
            if(customData.color != null)
            {
                CustomColor = ColorManager.ColorFromCustomDataColor(customData.color);
            }
            if(customData.easing != null)
            {
                TransitionEasing = Easings.EasingTypeFromString(customData.easing);
            }
            if(customData.lerpType != null)
            {
                HsvLerp = customData.lerpType == "HSV";
            }

            LightIDs = customData.lightID;
        }
    }


    public void MirrorColor()
    {
        //Mirrors the red/blue value of the event. Doesn't affect white or off
        switch(Value)
        {
            case LightEventValue.BlueOn:
                Value = LightEventValue.RedOn;
                break;
            case LightEventValue.BlueFlash:
                Value = LightEventValue.RedFlash;
                break;
            case LightEventValue.BlueFade:
                Value = LightEventValue.RedFade;
                break;
            case LightEventValue.BlueTransition:
                Value = LightEventValue.RedTransition;
                break;
            case LightEventValue.RedOn:
                Value = LightEventValue.BlueOn;
                break;
            case LightEventValue.RedFlash:
                Value = LightEventValue.BlueFlash;
                break;
            case LightEventValue.RedFade:
                Value = LightEventValue.BlueFade;
                break;
            case LightEventValue.RedTransition:
                Value = LightEventValue.BlueTransition;
                break;
        }
    }


    public bool AffectsID(int id)
    {
        if((LightIDs?.Length ?? 0) == 0)
        {
            return true;
        }
        return LightIDs.Contains(id);
    }


    public LightEvent GetLastEvent(int id)
    {
        if(AffectsID(id))
        {
            return this;
        }

        if(lastEvents != null && lastEvents.TryGetValue(id, out LightEvent lightEvent))
        {
            return lightEvent;
        }
        else return LastGlobalEvent;
    }


    public LightEvent GetNextEvent(int id)
    {
        if(nextEvents == null)
        {
            return null;
        }

        return nextEvents.TryGetValue(id, out LightEvent lightEvent) ? lightEvent : null;
    }


    public void SetLastEvent(int id, LightEvent lightEvent)
    {
        lastEvents ??= new Dictionary<int, LightEvent>();
        lastEvents[id] = lightEvent;
    }


    public void SetNextEvent(int id, LightEvent lightEvent)
    {
        nextEvents ??= new Dictionary<int, LightEvent>();
        nextEvents[id] = lightEvent;
    }
}


public class LightEventList : MapElementList<LightEvent>
{
    public void PrecalculateNeighboringEvents()
    {
        if(!IsSorted)
        {
            Debug.LogWarning("Trying to precalculate events in an unsorted list!");
            SortElementsByBeat();
        }

        //Precalculate the events that apply to each ID on this event
        //Helps speed up lightID at runtime
        LightEvent lastGlobalEvent = null;
        Dictionary<int, LightEvent> lastEvents = new Dictionary<int, LightEvent>();

        for(int i = 0; i < Elements.Count; i++)
        {
            LightEvent lightEvent = Elements[i];
            if((lightEvent.LightIDs?.Length ?? 0) == 0)
            {
                //This event affects all IDs, so we can close all previous events with this one
                if(lightEvent.IsTransition)
                {
                    //Only store next events if it's needed (if this is a transition event)
                    foreach(KeyValuePair<int, LightEvent> pair in lastEvents)
                    {
                        pair.Value.SetNextEvent(pair.Key, lightEvent);
                    }
                }

                lastEvents.Clear();
                lastGlobalEvent = lightEvent;

                continue;
            }

            foreach(KeyValuePair<int, LightEvent> pair in lastEvents)
            {
                int id = pair.Key;
                if(!lightEvent.AffectsID(id))
                {
                    lightEvent.SetLastEvent(id, pair.Value);
                }
            }

            lightEvent.LastGlobalEvent = lastGlobalEvent;

            if(!lightEvent.IsTransition)
            {
                //In this case, there's no need to add pointers from previous events
                foreach(int id in lightEvent.LightIDs)
                {
                    //Track the ids this event affects
                    lastEvents[id] = lightEvent;
                }
            }
            else
            {
                //This event is a transition, previous events also need a pointer to it
                foreach(int id in lightEvent.LightIDs)
                {
                    //Find the last event that affected this id, if any
                    LightEvent lastEvent = lastEvents.TryGetValue(id, out LightEvent x) ? x : lastGlobalEvent;

                    if(lastEvent != null)
                    {
                        //Add a reference to this event to the previous event
                        lastEvent.SetNextEvent(id, lightEvent);
                    }

                    lastEvents[id] = lightEvent;
                }
            }
        }
    }
}


public class LaserSpeedEvent : LightEvent
{
    public const float RotationSpeedMult = 20f;

    public float RotationSpeed;
    public bool LockRotation = false;
    public int Direction = -1;

    public List<LaserRotationData> RotationValues;


    public struct LaserRotationData
    {
        public float startPosition;
        public bool direction;
    }


    public LaserSpeedEvent() {}

    public LaserSpeedEvent(BeatmapBasicBeatmapEvent beatmapEvent)
    {
        Beat = beatmapEvent.b;
        Type = (LightEventType)beatmapEvent.et;
        Value = (LightEventValue)beatmapEvent.i;
        FloatValue = beatmapEvent.f;

        RotationSpeed = (int)Value;

        if(beatmapEvent.customData != null)
        {
            BeatmapCustomBasicEventData customData = beatmapEvent.customData;
            RotationSpeed = customData.speed ?? RotationSpeed;
            LockRotation = customData.lockRotation ?? false;
            Direction = customData.direction ?? -1;
        }


        RotationSpeed *= RotationSpeedMult;
    }


    public void PopulateRotationData(int laserCount, LaserSpeedEvent previous)
    {
        RotationValues = new List<LaserRotationData>();

        bool randomize = EnvironmentManager.CurrentEnvironmentParameters.RandomizeRotatingLasers;
        float startAngle = UnityEngine.Random.Range(0f, 360f);
        bool unifiedDirection = UnityEngine.Random.value >= 0.5f;

        for(int i = 0; i < laserCount; i++)
        {
            LaserRotationData newData = new LaserRotationData();
            if((int)Value > 0)
            {
                if(Direction < 0)
                {
                    if(randomize)
                    {
                        newData.direction = UnityEngine.Random.value >= 0.5f;
                    }
                    else
                    {
                        newData.direction = unifiedDirection;
                    }
                }
                else newData.direction = Direction == 1;

                if(randomize)
                {
                    newData.startPosition = UnityEngine.Random.Range(0f, 360f);
                }
                else
                {
                    float step = EnvironmentManager.CurrentEnvironmentParameters.RotatingLaserStep;
                    step *= newData.direction ? 1 : -1;
                    newData.startPosition = (startAngle + (step * i)) % 360f;
                }
            }
            else
            {
                newData.startPosition = 0f;
            }

            if(newData.startPosition < 0)
            {
                newData.startPosition += 360f;
            }

            if(LockRotation)
            {
                //Overwrite startPosition with the current laser position instead
                newData.startPosition = previous?.GetLaserRotation(Time, i) ?? 0f;
            }
            RotationValues.Add(newData);
        }
    }


    public float GetLaserRotation(float currentTime, int laserID)
    {
        if(laserID >= RotationValues.Count)
        {
            Debug.LogWarning($"Not enough randomized laser values to accomodate id {laserID}!");
            return 0f;
        }

        float timeDifference = currentTime - Time;

        LaserSpeedEvent.LaserRotationData rotationData = RotationValues[laserID];
        float rotationAmount = RotationSpeed * timeDifference;
        if(rotationData.direction)
        {
            //A true direction means clockwise rotation (negative euler angle)
            rotationAmount = -rotationAmount;
        }

        return (rotationData.startPosition + rotationAmount) % 360;
    }
}


public class BoostEvent : MapElement
{
    public bool Value;


    public BoostEvent(BeatmapColorBoostBeatmapEvent beatmapBoostEvent)
    {
        Beat = beatmapBoostEvent.b;
        Value = beatmapBoostEvent.o;
    }
}


public class LightingPropertyEventArgs
{
    public LightManager sender;

    public List<LightEvent> eventList;
    public LightEvent lightEvent;
    public LightEvent nextEvent;
    public LightEventType type;
    public int eventIndex;

    public Color laserColor;
    public Color glowColor;
}


public enum LightEventType
{
    BackLasers = 0,
    Rings = 1,
    LeftRotatingLasers = 2,
    RightRotatingLasers = 3,
    CenterLights = 4,
    LeftSideExtra = 6,
    RightSideExtra = 7,
    RingSpin = 8,
    RingZoom = 9,
    BillieLeftLasers = 10,
    BillieRightLasers = 11,
    LeftRotationSpeed = 12,
    RightRotationSpeed = 13,
    InterscopeHydraulicsDown = 16,
    InterscopeHydraulicsUp = 17,
    GagaLeftTowerHeight = 18,
    GagaRightTowerHeight = 19
}


public enum LightEventValue
{
    Off,
    BlueOn,
    BlueFlash,
    BlueFade,
    BlueTransition,
    RedOn,
    RedFlash,
    RedFade,
    RedTransition,
    WhiteOn,
    WhiteFlash,
    WhiteFade,
    WhiteTransition
}