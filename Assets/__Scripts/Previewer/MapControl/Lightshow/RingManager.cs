using System;
using System.Collections.Generic;
using UnityEngine;

public class RingManager : MonoBehaviour
{
    public static MapElementList<RingRotationEvent> SmallRingRotationEvents = new MapElementList<RingRotationEvent>();
    public static MapElementList<RingRotationEvent> BigRingRotationEvents = new MapElementList<RingRotationEvent>();

    public static MapElementList<RingZoomEvent> RingZoomEvents = new MapElementList<RingZoomEvent>();

    public static event Action<RingRotationEventArgs> OnRingRotationsChanged;
    public static event Action<float> OnRingZoomPositionChanged;

    public const float DefaultSmallRingRotationAmount = 90f;
    public const float DefaultSmallRingMaxStep = 5f;
    public const float SmallRingStartAngle = -45f;
    public const float SmallRingStartStep = 3f;

    public const float DefaultBigRingRotationAmount = 45f;
    public const float DefaultBigRingMaxStep = 5f;
    public const float BigRingStartAngle = -45f;
    public const float BigRingStartStep = 0f;

    public const float DefaultZoomSpeed = 1.5f;
    public const float DefaultCloseZoomStep = 2f;
    public const float DefaultFarZoomStep = 5f;
    public const bool StartRingZoomParity = true;
    public const float StartRingZoomStep = StartRingZoomParity ? DefaultFarZoomStep : DefaultCloseZoomStep;


    public static void UpdateRings()
    {
        UpdateRingZoom();
        UpdateRingRotations();
    }


    private static void UpdateRingRotations()
    {
        int lastIndex = SmallRingRotationEvents.GetLastIndex(TimeManager.CurrentTime, x => x.Beat <= TimeManager.CurrentBeat);

        RingRotationEventArgs eventArgs = new RingRotationEventArgs
        {
            events = SmallRingRotationEvents,
            currentEventIndex = lastIndex,
            affectBigRings = false
        };
        OnRingRotationsChanged?.Invoke(eventArgs);

        //Need to update big rings separately
        eventArgs.events = BigRingRotationEvents;
        eventArgs.currentEventIndex = BigRingRotationEvents.GetLastIndex(TimeManager.CurrentTime, x => x.Beat <= TimeManager.CurrentBeat);
        eventArgs.affectBigRings = true;
        OnRingRotationsChanged?.Invoke(eventArgs);
    }


    private static void UpdateRingZoom()
    {
        if(RingZoomEvents.Count == 0)
        {
            OnRingZoomPositionChanged?.Invoke(StartRingZoomStep);
            return;
        }

        int lastIndex = RingZoomEvents.GetLastIndex(TimeManager.CurrentTime, x => x.Beat <= TimeManager.CurrentBeat);
        if(lastIndex < 0)
        {
            //No ring zoom has taken affect, set defaults
            OnRingZoomPositionChanged?.Invoke(StartRingZoomStep);
            return;
        }

        RingZoomEvent current = RingZoomEvents[lastIndex];
        OnRingZoomPositionChanged?.Invoke(current.GetRingZoomStep(TimeManager.CurrentTime));
    }


    public static void SetStaticRings()
    {
        RingRotationEventArgs eventArgs = new RingRotationEventArgs
        {
            events = new List<RingRotationEvent>(),
            currentEventIndex = -1,
            affectBigRings = false
        };
        OnRingRotationsChanged?.Invoke(eventArgs);
        eventArgs.affectBigRings = true;
        OnRingRotationsChanged?.Invoke(eventArgs);

        OnRingZoomPositionChanged?.Invoke(StartRingZoomStep);
    }


    public static void PopulateRingEventData()
    {
        PopulateRingRotationEvents(ref SmallRingRotationEvents, DefaultSmallRingRotationAmount, DefaultSmallRingMaxStep, SmallRingStartAngle, SmallRingStartStep);
        PopulateRingRotationEvents(ref BigRingRotationEvents, DefaultBigRingRotationAmount, DefaultBigRingMaxStep, BigRingStartAngle, BigRingStartStep);

        PopulateRingZoomEvents();
    }


    private static void PopulateRingRotationEvents(ref MapElementList<RingRotationEvent> events, float rotationAmount, float maxStep, float startRotation, float startStep)
    {
        for(int i = 0; i < events.Count; i++)
        {
            RingRotationEvent current = events[i];
            if(i == 0)
            {
                //The first event should inherit the default starting positions
                current.StartAngle = startRotation;
                current.StartStep = startStep;

                current.InitializeValues(startRotation, rotationAmount, maxStep);
            }
            else
            {
                //Subsequent events get starting values based on the current ring rotations
                RingRotationEvent previous = events[i - 1];
                float timeDifference = current.Time - previous.Time;
                float rotationProgress = GetRingEventProgress(timeDifference, previous.Speed);

                current.StartAngle = previous.GetEventAngle(rotationProgress);
                current.StartStep = previous.GetEventStepAngle(rotationProgress);

                current.InitializeValues(previous.TargetAngle, rotationAmount, maxStep);
            }
        }
    }


    private static void PopulateRingZoomEvents()
    {
        for(int i = 0; i < RingZoomEvents.Count; i++)
        {
            RingZoomEvent current = RingZoomEvents[i];
            if(i == 0)
            {
                current.IsFarParity = !StartRingZoomParity;
                current.StartStep = StartRingZoomStep;
            }
            else
            {
                RingZoomEvent previous = RingZoomEvents[i - 1];

                current.IsFarParity = !previous.IsFarParity;
                current.StartStep = previous.GetRingZoomStep(current.Time);
            }
            if(!current.CustomStep)
            {
                //Avoid setting step if it's been set to a custom value through chroma
                current.Step = current.IsFarParity ? DefaultFarZoomStep : DefaultCloseZoomStep;
            }
        }
    }


    public static float GetRingEventProgress(float timeDifference, float speed)
    {
        return 1f - Mathf.Pow(2f, -(timeDifference * speed * 2f));
    }
}


public class RingRotationEvent : LightEvent
{
    public const float FixedDeltaTime = 1f / 60f;
    public const float DefaultSpeed = 2f;
    public const float DefaultProp = 1f;

    public float Rotation;
    public float Speed;
    public float Prop;
    public float Step;

    public float TargetAngle;
    public float StartAngle;
    public float StartStep;

    public bool CustomDirection = false;
    public bool CustomRotation = false;
    public bool CustomStep = false;

    private bool RotateClockwise;


    public RingRotationEvent() {}

    public RingRotationEvent(BeatmapBasicBeatmapEvent beatmapEvent)
    {
        Beat = beatmapEvent.b;
        Type = (LightEventType)beatmapEvent.et;
        Value = (LightEventValue)beatmapEvent.i;
        FloatValue = beatmapEvent.f;
        Speed = DefaultSpeed;
        Prop = DefaultProp;

        if(beatmapEvent.customData != null)
        {
            BeatmapCustomBasicEventData customData = beatmapEvent.customData;
            if(customData.direction != null)
            {
                RotateClockwise = customData.direction == 1;
                CustomDirection = true;
            }
            if(customData.rotation != null)
            {
                Rotation = Mathf.Abs((float)customData.rotation);
                CustomRotation = true;
            }
            if(customData.step != null)
            {
                Step = (float)customData.step;
                CustomStep = true;
            }
            Prop = customData.prop ?? Prop;
            Speed = customData.speed ?? Speed;
        }

        if(Prop == 0)
        {
            //Avoid divide by zero errors
            Prop = 0.0001f;
        }
    }


    public void InitializeValues(float startRotation, float defaultRotationAmount, float defaultMaxStep)
    {
        //Randomize the rotation and step values unless there are custom values set
        if(!CustomDirection)
        {
            RotateClockwise = UnityEngine.Random.value >= 0.5f;
        }
        if(!CustomRotation)
        {
            Rotation = defaultRotationAmount;
        }

        if(RotateClockwise)
        {
            Rotation = -Rotation;
        }

        if(!CustomStep)
        {
            Step = UnityEngine.Random.Range(-defaultMaxStep, defaultMaxStep);
        }
        TargetAngle = startRotation + Rotation;
    }


    public float StartInfluenceTime(int ringIndex)
    {
        //Returns the time where this event first starts affecting a given ring
        return Time + (FixedDeltaTime * ringIndex / Prop);
    }


    public float GetRingAngle(float currentTime, int ringIndex)
    {
        float ringInfluenceTime = StartInfluenceTime(ringIndex);
        float timeDifference = Mathf.Max(currentTime - ringInfluenceTime, 0f);

        float rotationProgress = RingManager.GetRingEventProgress(timeDifference, Speed);

        float angle = GetEventAngle(rotationProgress);
        float step = GetEventStepAngle(rotationProgress);
        return angle + (step * ringIndex);
    }


    public float GetEventAngle(float rotationProgress)
    {
        return Mathf.Lerp(StartAngle, TargetAngle, rotationProgress);
    }


    public float GetEventStepAngle(float rotationProgress)
    {
        return Mathf.Lerp(StartStep, Step, rotationProgress);
    }
}


public class RingZoomEvent : LightEvent
{
    public float Speed;
    public float Step;
    public float StartStep;

    public bool IsFarParity;

    public bool CustomStep = false;


    public RingZoomEvent() {}

    public RingZoomEvent(BeatmapBasicBeatmapEvent beatmapEvent)
    {
        Beat = beatmapEvent.b;
        Type = (LightEventType)beatmapEvent.et;
        Value = (LightEventValue)beatmapEvent.i;
        FloatValue = beatmapEvent.f;
        Speed = beatmapEvent.customData?.speed ?? RingManager.DefaultZoomSpeed;

        if(beatmapEvent.customData?.step != null)
        {
            Step = (float)beatmapEvent.customData.step;
            CustomStep = true;
        }
    }


    public float GetRingZoomStep(float currentTime)
    {
        float timeDifference = currentTime - Time;
        float zoomProgress = RingManager.GetRingEventProgress(timeDifference, Speed);

        return Mathf.Lerp(StartStep, Step, zoomProgress);
    }
}


public class RingRotationEventArgs
{
    public List<RingRotationEvent> events;
    public int currentEventIndex;
    public bool affectBigRings;
}