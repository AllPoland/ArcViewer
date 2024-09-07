using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightEvent : MapElement
{
    public LightEventType Type;
    public LightEventValue Value;
    public float FloatValue = 1f;

    public int? CustomColorIdx;
    public HashSet<int> LightIDs;
    public Easings.EasingType TransitionEasing;
    public bool HsvLerp = false;

    public LightEvent LastGlobalEvent;

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
                Color customColor = ColorManager.ColorFromCustomDataColor(customData.color);
                CustomColorIdx = LightColorManager.GetLightColorIdx(customColor);
            }
            if(customData.easing != null)
            {
                TransitionEasing = Easings.EasingTypeFromString(customData.easing);
            }
            if(customData.lerpType != null)
            {
                HsvLerp = customData.lerpType == "HSV";
            }

            //Use a hash set to store lightIDs to turn AffectsID() into O(1)
            LightIDs = customData.lightID?.ToHashSet();
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
        if((LightIDs?.Count ?? 0) == 0)
        {
            return true;
        }
        return LightIDs.Contains(id);
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
            LockRotation = customData.lockRotation;
            Direction = customData.direction ?? -1;
        }


        RotationSpeed *= RotationSpeedMult;
    }


    public void PopulateRotationData(int laserCount, LaserSpeedEvent previous)
    {
        RotationValues = new List<LaserRotationData>();

        bool randomize = EnvironmentManager.CurrentEnvironmentParameters.RandomizeRotatingLasers;
        float startAngle = Random.Range(0f, 360f);
        bool unifiedDirection = Random.value >= 0.5f;

        for(int i = 0; i < laserCount; i++)
        {
            LaserRotationData newData = new LaserRotationData();
            if((int)Value > 0)
            {
                if(Direction < 0)
                {
                    if(randomize)
                    {
                        newData.direction = Random.value >= 0.5f;
                    }
                    else
                    {
                        newData.direction = unifiedDirection;
                    }
                }
                else newData.direction = Direction == 1;

                if(randomize)
                {
                    newData.startPosition = Random.Range(0f, 360f);
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