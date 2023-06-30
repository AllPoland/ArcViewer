using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightManager : MonoBehaviour
{
    private static bool _staticLights;
    public static bool StaticLights
    {
        get => _staticLights || Scrubbing || SettingsManager.GetBool("staticlights");
        set
        {
            _staticLights = value;
            OnStaticLightsChanged?.Invoke();
        }
    }

    private static bool Scrubbing => TimeManager.Scrubbing && SettingsManager.GetBool("staticlightswhilescrubbing");

    private static bool _boostActive;
    public static bool BoostActive
    {
        get => _boostActive && !StaticLights;
        set
        {
            _boostActive = value;
        }
    }

    public static bool FlipBackLasers { get; private set; }

    public static event Action<LightingPropertyEventArgs> OnLightPropertiesChanged;
    public static event Action<LaserSpeedEvent, LightEventType> OnLaserRotationsChanged;
    public static event Action OnStaticLightsChanged;

    public const float FlashIntensity = 1.2f;

    private static ColorPalette colors => ColorManager.CurrentColors;
    private static Color lightColor1 => BoostActive ? colors.BoostLightColor1 : colors.LightColor1;
    private static Color lightColor2 => BoostActive ? colors.BoostLightColor2 : colors.LightColor2;
    private static Color whiteLightColor => BoostActive ? colors.BoostWhiteLightColor : colors.WhiteLightColor;

    private static MaterialPropertyBlock lightProperties;
    private static MaterialPropertyBlock glowProperties;

    private static readonly string[] lightSettings = new string[]
    {
        "staticlights",
        "lightglowbrightness"
    };

    private static readonly string[] v2Environments = new string[]
    {
        "DefaultEnvironment",
        "OriginsEnvironment",
        "TriangleEnvironment",
        "NiceEnvironment",
        "BigMirrorEnvironment",
        "DragonsEnvironment",
        "KDAEnvironment",
        "MonstercatEnvironment",
        "CrabRaveEnvironment",
        "PanicEnvironment",
        "RocketEnvironment",
        "GreenDayEnvironment",
        "GreenDayGrenadeEnvironment",
        "TimbalandEnvironment",
        "FitBeatEnvironment",
        "LinkinParkEnvironment",
        "BTSEnvironment",
        "KaleidoscopeEnvironment",
        "InterscopeEnvironment",
        "SkrillexEnvironment",
        "BillieEnvironment",
        "HalloweenEnvironment",
        "GagaEnvironment"
    };

    private static readonly string[] v3Environments = new string[]
    {
        "WeaveEnvironment",
        "PyroEnvironment",
        "EDMEnvironment",
        "TheSecondEnvironment",
        "LizzoEnvironment",
        "TheWeekndEnvironment",
        "RockMixtapeEnvironment",
        "Dragons2Environment",
        "Panic2Environment",
        "QueenEnvironment"
    };

    //These environments have red/blue flipped on their back/bottom lasers
    private static readonly string[] backLaserFlipEnvironments = new string[]
    {
        "DragonsEnvironment",
        "FitBeatEnvironment"
    };

    private static MapElementList<BoostEvent> boostEvents = new MapElementList<BoostEvent>();

    public MapElementList<LightEvent> backLaserEvents = new MapElementList<LightEvent>();
    public MapElementList<LightEvent> ringEvents = new MapElementList<LightEvent>();
    public MapElementList<LightEvent> leftLaserEvents = new MapElementList<LightEvent>();
    public MapElementList<LightEvent> rightLaserEvents = new MapElementList<LightEvent>();
    public MapElementList<LightEvent> centerLightEvents = new MapElementList<LightEvent>();

    public MapElementList<LaserSpeedEvent> leftLaserSpeedEvents = new MapElementList<LaserSpeedEvent>();
    public MapElementList<LaserSpeedEvent> rightLaserSpeedEvents = new MapElementList<LaserSpeedEvent>();

    [SerializeField, Range(0f, 1f)] private float lightSaturation;
    [SerializeField, Range(0f, 1f)] private float lightEmissionSaturation;
    [SerializeField] private float lightEmission;
    [SerializeField] private Color platformColor;


    public void UpdateLights(float beat)
    {
        if(StaticLights)
        {
            return;
        }

        int lastBoostEvent = boostEvents.GetLastIndex(TimeManager.CurrentTime, x => x.Beat <= beat);
        BoostActive = lastBoostEvent >= 0 ? boostEvents[lastBoostEvent].Value : false;

        UpdateLightEventType(LightEventType.BackLasers, backLaserEvents);
        UpdateLightEventType(LightEventType.Rings, ringEvents);
        UpdateLightEventType(LightEventType.LeftRotatingLasers, leftLaserEvents);
        UpdateLightEventType(LightEventType.RightRotatingLasers, rightLaserEvents);
        UpdateLightEventType(LightEventType.CenterLights, centerLightEvents);

        UpdateLaserSpeedEventType(LightEventType.LeftRotationSpeed, leftLaserSpeedEvents);
        UpdateLaserSpeedEventType(LightEventType.RightRotationSpeed, rightLaserSpeedEvents);

        RingManager.UpdateRings();
    }


    private void UpdateLightEventType(LightEventType type, MapElementList<LightEvent> events)
    {
        int lastIndex = events.GetLastIndex(TimeManager.CurrentTime, x => x.Time <= TimeManager.CurrentTime);
        bool foundEvent = lastIndex >= 0;

        LightEvent currentEvent = foundEvent ? events[lastIndex] : null;

        bool hasNextEvent = lastIndex + 1 < events.Count;
        LightEvent nextEvent = hasNextEvent ? events[lastIndex + 1] : null;

        UpdateLightEvent(type, currentEvent, nextEvent, events, lastIndex);
    }


    private void UpdateLightEvent(LightEventType type, LightEvent lightEvent, LightEvent nextEvent, MapElementList<LightEvent> events, int eventIndex)
    {
        Color eventColor = GetEventColor(lightEvent, nextEvent);
        SetLightProperties(eventColor, ref lightProperties, ref glowProperties);

        LightingPropertyEventArgs eventArgs = new LightingPropertyEventArgs
        {
            sender = this,
            eventList = events,
            lightEvent = lightEvent,
            nextEvent = nextEvent,
            type = type,
            eventIndex = eventIndex,
            laserProperties = lightProperties,
            glowProperties = glowProperties
        };
        OnLightPropertiesChanged?.Invoke(eventArgs);
    }


    private void UpdateLaserSpeedEventType(LightEventType type, MapElementList<LaserSpeedEvent> events)
    {
        int lastIndex = events.GetLastIndex(TimeManager.CurrentTime, x => x.Time <= TimeManager.CurrentTime);
        bool foundEvent = lastIndex >= 0;

        LaserSpeedEvent lastEvent = foundEvent ? events[lastIndex] : null;
        OnLaserRotationsChanged?.Invoke(lastEvent, type);
    }


    public void SetLightProperties(Color baseColor, ref MaterialPropertyBlock laserProperties, ref MaterialPropertyBlock laserGlowProperties)
    {
        laserProperties.SetColor("_BaseColor", GetLightColor(baseColor));
        laserProperties.SetColor("_EmissionColor", GetLightEmission(baseColor));

        laserGlowProperties.SetColor("_BaseColor", baseColor);
        laserGlowProperties.SetFloat("_Alpha", baseColor.a * SettingsManager.GetFloat("lightglowbrightness"));
    }


    private Color GetLightColor(Color baseColor)
    {
        float s;
        Color.RGBToHSV(baseColor, out _, out s, out _);
        Color newColor = baseColor.SetSaturation(s * lightSaturation);
        newColor.a = Mathf.Clamp(baseColor.a, 0f, 1f);
        return newColor;
    }


    private Color GetLightEmission(Color baseColor)
    {
        float emission = lightEmission * baseColor.a;

        float h, s;
        Color.RGBToHSV(baseColor, out h, out s, out _);
        Color newColor = baseColor.SetHSV(h, s * lightEmissionSaturation, emission, true);
        return newColor;
    }


    public static Color GetEventColor(LightEvent lightEvent, LightEvent nextEvent)
    {
        if(lightEvent == null)
        {
            return Color.clear;
        }

        switch(lightEvent.Value)
        {
            case LightEventValue.RedOn:
            case LightEventValue.RedTransition:
            case LightEventValue.BlueOn:
            case LightEventValue.BlueTransition:
            case LightEventValue.WhiteOn:
            case LightEventValue.WhiteTransition:
            case LightEventValue.Off:
                return GetStandardEventColor(lightEvent, nextEvent);

            case LightEventValue.RedFlash:
            case LightEventValue.WhiteFlash:
            case LightEventValue.BlueFlash:
                return GetFlashColor(lightEvent);

            case LightEventValue.RedFade:
            case LightEventValue.BlueFade:
            case LightEventValue.WhiteFade:
                return GetFadeColor(lightEvent);

            default:
                return Color.clear;
        }
    }


    private static Color GetEventBaseColor(LightEvent lightEvent)
    {
        Color baseColor;
        if(lightEvent.CustomColor != null)
        {
            baseColor = (Color)lightEvent.CustomColor;
        }
        else
        {
            switch(lightEvent.Value)
            {
                case LightEventValue.RedOn:
                case LightEventValue.RedTransition:
                case LightEventValue.RedFlash:
                case LightEventValue.RedFade:
                    baseColor = lightColor1;
                    break;
                case LightEventValue.BlueOn:
                case LightEventValue.BlueTransition:
                case LightEventValue.BlueFlash:
                case LightEventValue.BlueFade:
                    baseColor = lightColor2;
                    break;
                case LightEventValue.WhiteOn:
                case LightEventValue.WhiteTransition:
                case LightEventValue.WhiteFlash:
                case LightEventValue.WhiteFade:
                    baseColor = whiteLightColor;
                    break;
                case LightEventValue.Off:
                default:
                    baseColor = Color.clear;
                    break;
            }
        }
        baseColor.a *= lightEvent.FloatValue;
        return baseColor;
    }


    private static Color GetStandardEventColor(LightEvent lightEvent, LightEvent nextEvent)
    {
        Color baseColor = GetEventBaseColor(lightEvent);

        if(nextEvent?.isTransition ?? false)
        {
            Color transitionColor = GetEventBaseColor(nextEvent);
            if(lightEvent.Value == LightEventValue.Off)
            {
                //Off events inherit the color they transition to
                baseColor = transitionColor;
                baseColor.a = 0f;
            }

            float transitionTime = nextEvent.Time - lightEvent.Time;
            float t = (TimeManager.CurrentTime - lightEvent.Time) / transitionTime;
            t = lightEvent.TransitionEasing(t);

            if(lightEvent.HsvLerp)
            {
                baseColor = baseColor.LerpHSV(transitionColor, t);
            }
            else baseColor = Color.LerpUnclamped(baseColor, transitionColor, t);
        }
        return baseColor;
    }


    private static Color GetFlashColor(LightEvent lightEvent)
    {
        const float fadeTime = 0.6f;
        Color baseColor = GetEventBaseColor(lightEvent);

        float timeDifference = TimeManager.CurrentTime - lightEvent.Time;
        if(timeDifference < fadeTime)
        {
            float t = timeDifference / fadeTime;
            baseColor.a *= Mathf.Lerp(FlashIntensity, 1f, Easings.Cubic.Out(t));
        }
        return baseColor;
    }


    private static Color GetFadeColor(LightEvent lightEvent)
    {
        const float fadeTime = 1.5f;
        Color baseColor = GetEventBaseColor(lightEvent);

        float floatValue = lightEvent.FloatValue;
        float timeDifference = TimeManager.CurrentTime - lightEvent.Time;
        if(timeDifference >= fadeTime)
        {
            baseColor.a = 0f;
        }
        else
        {
            float t = timeDifference / fadeTime;
            baseColor.a *= Mathf.Lerp(FlashIntensity, 0f, Easings.Expo.Out(t));
        }
        return baseColor;
    }


    private void SetStaticLayout()
    {
        LightEvent backLasers = new LightEvent
        {
            Beat = 0f,
            Type = LightEventType.BackLasers,
            Value = LightEventValue.BlueOn
        };
        LightEvent rings = new LightEvent
        {
            Beat = 0f,
            Type = LightEventType.Rings,
            Value = LightEventValue.BlueOn
        };
        LightEvent leftLasers = new LightEvent
        {
            Beat = 0f,
            Type = LightEventType.LeftRotatingLasers,
            Value = LightEventValue.Off
        };
        LightEvent rightLasers = new LightEvent
        {
            Beat = 0f,
            Type = LightEventType.RightRotatingLasers,
            Value = LightEventValue.Off
        };
        LightEvent centerLights = new LightEvent
        {
            Beat = 0f,
            Type = LightEventType.CenterLights,
            Value = LightEventValue.BlueOn
        };

        UpdateLightEvent(LightEventType.BackLasers, backLasers, null, new MapElementList<LightEvent>(), -1);
        UpdateLightEvent(LightEventType.Rings, rings, null, new MapElementList<LightEvent>(), -1);
        UpdateLightEvent(LightEventType.LeftRotatingLasers, leftLasers, null, new MapElementList<LightEvent>(), -1);
        UpdateLightEvent(LightEventType.RightRotatingLasers, rightLasers, null, new MapElementList<LightEvent>(), -1);
        UpdateLightEvent(LightEventType.CenterLights, centerLights, null, new MapElementList<LightEvent>(), -1);

        OnLaserRotationsChanged?.Invoke(null, LightEventType.LeftRotationSpeed);
        OnLaserRotationsChanged?.Invoke(null, LightEventType.RightRotationSpeed);

        RingManager.SetStaticRings();
    }


    public void UpdateLightParameters()
    {
        if(StaticLights)
        {
            SetStaticLayout();
        }
        else UpdateLights(TimeManager.CurrentBeat);
    }


    public void UpdateSettings(string setting)
    {
        if(setting == "all" || lightSettings.Contains(setting))
        {
            UpdateLightParameters();
        }
    }


    public void UpdatePlaying(bool playing)
    {
        if(playing)
        {
            return;
        }

        if(Scrubbing)
        {
            SetStaticLayout();
        }
        else UpdateLightParameters();
    }


    public void UpdateDifficulty(Difficulty newDifficulty)
    {
        string environmentName = BeatmapManager.EnvironmentName;
        if(newDifficulty.beatmapDifficulty.basicBeatMapEvents.Length == 0 || v3Environments.Contains(environmentName))
        {
            StaticLights = true;
            return;
        }
        else StaticLights = false;

        FlipBackLasers = backLaserFlipEnvironments.Contains(environmentName);

        boostEvents.Clear();
        foreach(BeatmapColorBoostBeatmapEvent beatmapBoostEvent in newDifficulty.beatmapDifficulty.colorBoostBeatMapEvents)
        {
            boostEvents.Add(new BoostEvent(beatmapBoostEvent));
        }
        boostEvents.SortElementsByBeat();

        backLaserEvents.Clear();
        ringEvents.Clear();
        leftLaserEvents.Clear();
        rightLaserEvents.Clear();
        centerLightEvents.Clear();

        leftLaserSpeedEvents.Clear();
        rightLaserSpeedEvents.Clear();

        RingManager.SmallRingRotationEvents.Clear();
        RingManager.BigRingRotationEvents.Clear();

        RingManager.RingZoomEvents.Clear();

        foreach(BeatmapBasicBeatmapEvent beatmapEvent in newDifficulty.beatmapDifficulty.basicBeatMapEvents)
        {
            AddLightEvent(beatmapEvent);
        }

        backLaserEvents.SortElementsByBeat();
        ringEvents.SortElementsByBeat();
        leftLaserEvents.SortElementsByBeat();
        rightLaserEvents.SortElementsByBeat();
        centerLightEvents.SortElementsByBeat();

        leftLaserSpeedEvents.SortElementsByBeat();
        rightLaserEvents.SortElementsByBeat();
        
        RingManager.SmallRingRotationEvents.SortElementsByBeat();
        RingManager.BigRingRotationEvents.SortElementsByBeat();

        RingManager.RingZoomEvents.SortElementsByBeat();

        PopulateLaserRotationEventData();
        RingManager.PopulateRingEventData();

        UpdateLights(TimeManager.CurrentBeat);
    }


    private void AddLightEvent(BeatmapBasicBeatmapEvent beatmapEvent)
    {
        switch((LightEventType)beatmapEvent.et)
        {
            case LightEventType.BackLasers:
                backLaserEvents.Add(new LightEvent(beatmapEvent));
                break;
            case LightEventType.Rings:
                ringEvents.Add(new LightEvent(beatmapEvent));
                break;
            case LightEventType.LeftRotatingLasers:
                leftLaserEvents.Add(new LightEvent(beatmapEvent));
                break;
            case LightEventType.RightRotatingLasers:
                rightLaserEvents.Add(new LightEvent(beatmapEvent));
                break;
            case LightEventType.CenterLights:
                centerLightEvents.Add(new LightEvent(beatmapEvent));
                break;
            case LightEventType.LeftRotationSpeed:
                leftLaserSpeedEvents.Add(new LaserSpeedEvent(beatmapEvent));
                break;
            case LightEventType.RightRotationSpeed:
                rightLaserSpeedEvents.Add(new LaserSpeedEvent(beatmapEvent));
                break;
            case LightEventType.RingSpin:
                //Account for name filters for rotation events that only effect big/small rings
                if(string.IsNullOrEmpty(beatmapEvent.customData?.nameFilter) || beatmapEvent.customData.nameFilter == "SmallTrackLaneRings")
                {
                    RingManager.SmallRingRotationEvents.Add(new RingRotationEvent(beatmapEvent));
                }
                if(string.IsNullOrEmpty(beatmapEvent.customData?.nameFilter) || beatmapEvent.customData.nameFilter == "BigTrackLaneRings")
                {
                    RingManager.BigRingRotationEvents.Add(new RingRotationEvent(beatmapEvent));
                }
                break;
            case LightEventType.RingZoom:
                RingManager.RingZoomEvents.Add(new RingZoomEvent(beatmapEvent));
                break;
        }
    }


    private void PopulateLaserRotationEventData()
    {
        const int laserCount = 4;
        for(int i = 0; i < leftLaserSpeedEvents.Count; i++)
        {
            LaserSpeedEvent previous = i > 0 ? leftLaserSpeedEvents[i - 1] : null;
            leftLaserSpeedEvents[i].PopulateRotationData(laserCount, previous);
        }

        int x = 0;
        for(int i = 0; i < rightLaserSpeedEvents.Count; i++)
        {
            LaserSpeedEvent speedEvent = rightLaserSpeedEvents[i];
            LaserSpeedEvent previous = i > 0 ? rightLaserSpeedEvents[i - 1] : null;

            if(speedEvent.Value == 0 || x >= leftLaserSpeedEvents.Count || speedEvent.LockRotation)
            {
                //No left laser events left to check,
                //or we specifically need to get specific values for this event
                speedEvent.PopulateRotationData(laserCount, previous);
                continue;
            }

            speedEvent.RotationValues = new List<LaserSpeedEvent.LaserRotationData>();

            //Find a left laser event (if any) that matches this event's beat
            while(x < leftLaserSpeedEvents.Count)
            {
                LaserSpeedEvent leftSpeedEvent = leftLaserSpeedEvents[x];

                if(ObjectManager.CheckSameTime(speedEvent.Time, leftSpeedEvent.Time))
                {
                    //Events on the same time get the same parameters
                    //unless the left event is 0 speed,
                    //or they have different specified directions through chroma
                    if(leftSpeedEvent.Value != 0 && speedEvent.Direction == leftSpeedEvent.Direction)
                    {
                        speedEvent.RotationValues.AddRange(leftSpeedEvent.RotationValues);
                    }
                    break;
                }
                else if(leftSpeedEvent.Beat >= speedEvent.Beat)
                {
                    //We've passed this event's time, so there's no event sharing this beat
                    break;
                }
                else x++;
            }

            if(speedEvent.RotationValues.Count < laserCount)
            {
                //No left laser event was found on this beat, so randomize this event
                speedEvent.PopulateRotationData(laserCount, previous);
            }
        }
    }


    private void Start()
    {
        //Using this event instead of BeatmapManager.OnDifficultyChanged
        //ensures that bpm changes are loaded before precalculating event times
        TimeManager.OnDifficultyBpmEventsLoaded += UpdateDifficulty;
        TimeManager.OnBeatChanged += UpdateLights;
        TimeManager.OnPlayingChanged += UpdatePlaying;

        SettingsManager.OnSettingsUpdated += UpdateSettings;
        ColorManager.OnColorsChanged += (_) => UpdateLightParameters();

        OnStaticLightsChanged += UpdateLightParameters;
        StaticLights = true;
    }


    private void Awake()
    {
        lightProperties = new MaterialPropertyBlock();
        glowProperties = new MaterialPropertyBlock();
    }
}


public class LightEvent : MapElement
{
    public LightEventType Type;
    public LightEventValue Value;
    public float FloatValue = 1f;

    public Color? CustomColor;
    List<int> LightIDs = new List<int>();
    public Easings.EasingDelegate TransitionEasing;
    public bool HsvLerp = false;

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
                LightIDs = new List<int>(customData.lightID);
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
        return LightIDs.Count == 0 || LightIDs.Contains(id);
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