using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightEvent : MapElement
{
    public LightEventType Type;
    public LightEventValue Value;
    public float FloatValue = 1f;

    public Color? CustomColor;
    public Dictionary<int, bool> lightIDs;
    public Easings.EasingDelegate TransitionEasing;
    public bool HsvLerp = false;

    public Dictionary<int, LightEvent> lastEvents = new Dictionary<int, LightEvent>();
    public Dictionary<int, LightEvent> nextEvents = new Dictionary<int, LightEvent>();

    public bool isTransition => Value == LightEventValue.RedTransition || Value == LightEventValue.BlueTransition || Value == LightEventValue.WhiteTransition;


    public LightEvent() {}

    public LightEvent(BeatmapBasicBeatmapEvent beatmapEvent)
    {
        Beat = beatmapEvent.b;
        Type = (LightEventType)beatmapEvent.et;
        Value = (LightEventValue)beatmapEvent.i;
        FloatValue = beatmapEvent.f;
        TransitionEasing = Easings.Linear;

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
            if(customData.lightID != null)
            {
                lightIDs = new Dictionary<int, bool>();
                foreach(int id in customData.lightID)
                {
                    lightIDs.TryAdd(id, true);
                }
            }
            if(customData.easing != null)
            {
                TransitionEasing = Easings.EasingFromString(customData.easing);
            }
            if(customData.lerpType != null)
            {
                HsvLerp = customData.lerpType == "HSV";
            }
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
        if(lightIDs == null)
        {
            return true;
        }
        return lightIDs.TryGetValue(id, out bool value) ? value : false;
    }


    public LightEvent GetLastEvent(int id)
    {
        return lastEvents.TryGetValue(id, out LightEvent lightEvent)
            ? lightEvent
            : AffectsID(id) ? this : null;
    }


    public LightEvent GetNextEvent(int id)
    {
        return nextEvents.TryGetValue(id, out LightEvent lightEvent) ? lightEvent : null;
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
        Dictionary<int, LightEvent> lastEvents = new Dictionary<int, LightEvent>();
        for(int i = 0; i < Elements.Count; i++)
        {
            LightEvent lightEvent = Elements[i];
            if(lightEvent.lightIDs == null)
            {
                //This event affects all IDs, so we can close all previous events with this one
                foreach(KeyValuePair<int, LightEvent> value in lastEvents)
                {
                    int id = value.Key;
                    LightEvent lastEvent = value.Value;

                    lastEvent.nextEvents[id] = lightEvent;
                }
                lastEvents.Clear();
                continue;
            }

            lightEvent.lastEvents = lastEvents.ToDictionary(x => x.Key, x => x.Value);

            foreach(KeyValuePair<int, bool> value in lightEvent.lightIDs)
            {
                int id = value.Key;

                LightEvent lastEvent;
                if(lastEvents.Count == 0)
                {
                    lastEvent = i > 0 ? Elements[i - 1] : null;
                }
                else lastEvent = lastEvents.TryGetValue(id, out LightEvent x) ? x : null;

                if(lastEvent != null)
                {
                    lastEvent.nextEvents[id] = lightEvent;
                }
                lastEvents[id] = lightEvent;
            }
        }
    }
}


public class LaserSpeedEvent : LightEvent
{
    public const float DefaultRotationSpeedMult = 20f;

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

        RotationSpeed = (int)Value * DefaultRotationSpeedMult;

        if(beatmapEvent.customData != null)
        {
            BeatmapCustomBasicEventData customData = beatmapEvent.customData;
            RotationSpeed = customData.speed ?? RotationSpeed;
            LockRotation = customData.lockRotation ?? false;
            Direction = customData.direction ?? -1;
        }
    }


    public void PopulateRotationData(int laserCount, LaserSpeedEvent previous)
    {
        RotationValues = new List<LaserRotationData>();
        for(int i = 0; i < laserCount; i++)
        {
            LaserRotationData newData = new LaserRotationData();
            if((int)Value > 0)
            {
                newData.startPosition = UnityEngine.Random.Range(0f, 360f);

                if(Direction < 0)
                {
                    newData.direction = UnityEngine.Random.value >= 0.5f;
                }
                else newData.direction = Direction == 1;
            }
            else
            {
                newData.startPosition = 0f;
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

    public MaterialPropertyBlock laserProperties;
    public MaterialPropertyBlock glowProperties;
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