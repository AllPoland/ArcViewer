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

    public static event Action<LightingPropertyEventArgs> OnLightPropertiesChanged;
    public static event Action<LaserSpeedEvent, LightEventType> OnLaserRotationsChanged;
    public static event Action OnStaticLightsChanged;

    public const float FlashIntensity = 1.2f;

    private static ColorPalette colors => ColorManager.CurrentColors;
    private static Color lightColor1 => BoostActive ? colors.BoostLightColor1 : colors.LightColor1;
    private static  Color lightColor2 => BoostActive ? colors.BoostLightColor2 : colors.LightColor2;
    private static Color whiteLightColor => BoostActive ? colors.BoostWhiteLightColor : colors.WhiteLightColor;

    private static BeatmapColorBoostBeatmapEvent[] boostEvents => BeatmapManager.CurrentDifficulty.beatmapDifficulty.colorBoostBeatMapEvents;

    private static MaterialPropertyBlock lightProperties;
    private static MaterialPropertyBlock persistentLightProperties;
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
        "Panic2Environment"
    };

    public List<LightEvent> backLaserEvents = new List<LightEvent>();
    public List<LightEvent> ringEvents = new List<LightEvent>();
    public List<LightEvent> leftLaserEvents = new List<LightEvent>();
    public List<LightEvent> rightLaserEvents = new List<LightEvent>();
    public List<LightEvent> centerLightEvents = new List<LightEvent>();

    public List<LaserSpeedEvent> leftLaserSpeedEvents = new List<LaserSpeedEvent>();
    public List<LaserSpeedEvent> rightLaserSpeedEvents = new List<LaserSpeedEvent>();

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

        BoostActive = boostEvents.LastOrDefault(x => x.b <= beat)?.o ?? false;

        UpdateLightEventType(LightEventType.BackLasers, backLaserEvents, beat);
        UpdateLightEventType(LightEventType.Rings, ringEvents, beat);
        UpdateLightEventType(LightEventType.LeftRotatingLasers, leftLaserEvents, beat);
        UpdateLightEventType(LightEventType.RightRotatingLasers, rightLaserEvents, beat);
        UpdateLightEventType(LightEventType.CenterLights, centerLightEvents, beat);

        UpdateLaserSpeedEventType(LightEventType.LeftRotationSpeed, leftLaserSpeedEvents, beat);
        UpdateLaserSpeedEventType(LightEventType.RightRotationSpeed, rightLaserSpeedEvents, beat);

        RingManager.UpdateRings();
    }


    private void UpdateLightEventType(LightEventType type, List<LightEvent> events, float beat)
    {
        int lastIndex = events.FindLastIndex(x => x.Beat <= beat);
        bool foundEvent = lastIndex >= 0;

        LightEvent currentEvent = foundEvent ? events[lastIndex] : null;

        bool hasNextEvent = lastIndex + 1 < events.Count;
        LightEvent nextEvent = hasNextEvent ? events[lastIndex + 1] : null;

        UpdateLightEvent(type, currentEvent, nextEvent);
    }


    private void UpdateLightEvent(LightEventType type, LightEvent lightEvent, LightEvent nextEvent)
    {
        LightEventValue value = lightEvent?.Value ?? LightEventValue.Off;
        Color baseColor = GetEventColor(value, lightEvent, nextEvent);
        SetLightProperties(baseColor);

        LightingPropertyEventArgs eventArgs = new LightingPropertyEventArgs
        {
            laserProperties = lightProperties,
            persistentLaserProperties = persistentLightProperties,
            glowProperties = glowProperties,
            type = type
        };
        OnLightPropertiesChanged?.Invoke(eventArgs);
    }


    private void UpdateLaserSpeedEventType(LightEventType type, List<LaserSpeedEvent> events, float beat)
    {
        LaserSpeedEvent lastEvent = events.LastOrDefault(x => x.Beat <= beat);
        OnLaserRotationsChanged?.Invoke(lastEvent, type);
    }


    private void SetLightProperties(Color baseColor)
    {
        glowProperties.SetColor("_BaseColor", baseColor);
        glowProperties.SetFloat("_Alpha", baseColor.a * SettingsManager.GetFloat("lightglowbrightness"));

        Color lightColor = GetLightColor(baseColor);
        Color emissionColor = GetLightEmission(baseColor);

        persistentLightProperties.SetColor("_BaseColor", GetPersistentLightColor(lightColor));
        persistentLightProperties.SetColor("_EmissionColor", emissionColor);

        lightProperties.SetColor("_BaseColor", lightColor);
        lightProperties.SetColor("_EmissionColor", emissionColor);
    }


    private Color GetLightColor(Color baseColor)
    {
        float s;
        Color.RGBToHSV(baseColor, out _, out s, out _);
        Color newColor = baseColor.SetSaturation(s * lightSaturation);
        newColor.a = Mathf.Clamp(baseColor.a, 0f, 1f);
        return newColor;
    }


    private Color GetPersistentLightColor(Color baseColor)
    {
        Color newColor = Color.Lerp(platformColor, baseColor, baseColor.a);
        newColor.a = 1f;
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


    private Color GetEventColor(LightEventValue value, LightEvent lightEvent, LightEvent nextEvent)
    {
        switch(value)
        {
            case LightEventValue.RedOn:
            case LightEventValue.RedTransition:
                //We can be sure lightEvent isn't null because value would be set to Off
                return GetOnColor(lightEvent, lightColor1, nextEvent);
            case LightEventValue.RedFlash:
                return GetFlashColor(lightEvent, lightColor1);
            case LightEventValue.RedFade:
                return GetFadeColor(lightEvent, lightColor1);

            case LightEventValue.BlueOn:
            case LightEventValue.BlueTransition:
                return GetOnColor(lightEvent, lightColor2, nextEvent);
            case LightEventValue.BlueFlash:
                return GetFlashColor(lightEvent, lightColor2);
            case LightEventValue.BlueFade:
                return GetFadeColor(lightEvent, lightColor2);

            case LightEventValue.WhiteOn:
            case LightEventValue.WhiteTransition:
                return GetOnColor(lightEvent, whiteLightColor, nextEvent);
            case LightEventValue.WhiteFlash:
                return GetFlashColor(lightEvent, whiteLightColor);
            case LightEventValue.WhiteFade:
                return GetFadeColor(lightEvent, whiteLightColor);

            case LightEventValue.Off:
            default:
                return new Color(0f, 0f, 0f, 0f);
        }
    }


    private Color GetEventBaseColor(LightEvent lightEvent)
    {
        Color baseColor = new Color();
        switch(lightEvent.Value)
        {
            case LightEventValue.RedOn:
            case LightEventValue.RedTransition:
                baseColor = lightColor1;
                break;
            case LightEventValue.BlueOn:
            case LightEventValue.BlueTransition:
                baseColor = lightColor2;
                break;
            case LightEventValue.WhiteOn:
            case LightEventValue.WhiteTransition:
                baseColor = whiteLightColor;
                break;
            default:
                return new Color(0f, 0f, 0f, 0f);
        }
        baseColor.a = lightEvent.FloatValue;
        return baseColor;
    }


    private Color GetOnColor(LightEvent lightEvent, Color baseColor, LightEvent nextEvent)
    {
        baseColor.a = lightEvent.FloatValue;

        if(nextEvent?.isTransition ?? false)
        {
            Color transitionColor = GetEventBaseColor(nextEvent);
            float transitionTime = nextEvent.Time - lightEvent.Time;

            float t = (TimeManager.CurrentTime - lightEvent.Time) / transitionTime;
            baseColor = Color.Lerp(baseColor, transitionColor, t);
        }

        return baseColor;
    }


    private Color GetFlashColor(LightEvent lightEvent, Color baseColor)
    {
        const float fadeTime = 0.6f;

        float floatValue = lightEvent.FloatValue;
        float timeDifference = TimeManager.CurrentTime - lightEvent.Time;
        if(timeDifference >= fadeTime)
        {
            baseColor.a = floatValue;
        }
        else
        {
            float t = timeDifference / fadeTime;
            float flashBrightness = floatValue * FlashIntensity;
            baseColor.a = Mathf.Lerp(flashBrightness, floatValue, Easings.Cubic.Out(t));
        }
        return baseColor;
    }


    private Color GetFadeColor(LightEvent lightEvent, Color baseColor)
    {
        const float fadeTime = 1.5f;

        float floatValue = lightEvent.FloatValue;
        float timeDifference = TimeManager.CurrentTime - lightEvent.Time;
        if(timeDifference >= fadeTime)
        {
            baseColor.a = 0f;
        }
        else
        {
            float t = timeDifference / fadeTime;
            float flashBrightness = floatValue * FlashIntensity;
            baseColor.a = Mathf.Lerp(flashBrightness, 0f, Easings.Expo.Out(t));
        }
        return baseColor;
    }


    private float GetV1TransitionAlpha(float startAlpha, float endAlpha, float fadeTime, float eventTime)
    {
        
        return 0f;
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

        UpdateLightEvent(LightEventType.BackLasers, backLasers, null);
        UpdateLightEvent(LightEventType.Rings, rings, null);
        UpdateLightEvent(LightEventType.LeftRotatingLasers, leftLasers, null);
        UpdateLightEvent(LightEventType.RightRotatingLasers, rightLasers, null);
        UpdateLightEvent(LightEventType.CenterLights, centerLights, null);

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
        string environmentName = BeatmapManager.Info._environmentName;
        if(newDifficulty.beatmapDifficulty.basicBeatMapEvents.Length == 0 || v3Environments.Contains(environmentName))
        {
            StaticLights = true;
            return;
        }
        else StaticLights = false;

        backLaserEvents.Clear();
        ringEvents.Clear();
        leftLaserEvents.Clear();
        rightLaserEvents.Clear();
        centerLightEvents.Clear();

        leftLaserSpeedEvents.Clear();
        rightLaserSpeedEvents.Clear();

        RingManager.SmallRingRotationEvents.Clear();
        RingManager.BigRingRotationEvents.Clear();

        foreach(BeatmapBasicBeatmapEvent beatmapEvent in newDifficulty.beatmapDifficulty.basicBeatMapEvents)
        {
            AddLightEvent(beatmapEvent);
        }

        backLaserEvents = SortLightsByBeat(backLaserEvents);
        ringEvents = SortLightsByBeat(ringEvents);
        leftLaserEvents = SortLightsByBeat(leftLaserEvents);
        rightLaserEvents = SortLightsByBeat(rightLaserEvents);
        centerLightEvents = SortLightsByBeat(centerLightEvents);

        leftLaserSpeedEvents = SortLightsByBeat(leftLaserSpeedEvents);
        rightLaserEvents = SortLightsByBeat(rightLaserEvents);
        
        RingManager.SmallRingRotationEvents = SortLightsByBeat(RingManager.SmallRingRotationEvents);
        RingManager.BigRingRotationEvents = SortLightsByBeat(RingManager.BigRingRotationEvents);

        PopulateLaserRotationEventData();
        RingManager.PopulateRingEventData();

        UpdateLights(TimeManager.CurrentBeat);
    }


    private static List<T> SortLightsByBeat<T>(List<T> events) where T : LightEvent
    {
        return events.OrderBy(x => x.Beat).ToList();
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
                RingManager.SmallRingRotationEvents.Add(new RingRotationEvent(beatmapEvent));
                RingManager.BigRingRotationEvents.Add(new RingRotationEvent(beatmapEvent));
                break;
            case LightEventType.RingZoom:
                RingManager.RingZoomEvents.Add(new RingZoomEvent(beatmapEvent));
                break;
        }
    }


    private void PopulateLaserRotationEventData()
    {
        const int laserCount = 4;
        foreach(LaserSpeedEvent speedEvent in leftLaserSpeedEvents)
        {
            speedEvent.PopulateRotationData(laserCount);
        }

        int i = 0;
        foreach(LaserSpeedEvent speedEvent in rightLaserSpeedEvents)
        {
            if(i >= leftLaserSpeedEvents.Count)
            {
                speedEvent.PopulateRotationData(laserCount);
                continue;
            }

            speedEvent.rotationValues = new List<LaserSpeedEvent.LaserRotationData>();

            //Find a left laser event (if any) that matches this event's beat
            while(i < leftLaserSpeedEvents.Count)
            {
                LaserSpeedEvent leftSpeedEvent = leftLaserSpeedEvents[i];

                if(ObjectManager.CheckSameTime(speedEvent.Time, leftSpeedEvent.Time))
                {
                    //Events on the same time get the same parameters
                    speedEvent.rotationValues.AddRange(leftSpeedEvent.rotationValues);
                    break;
                }
                else if(leftSpeedEvent.Beat >= speedEvent.Beat)
                {
                    //We've passed this event's time, so there's no event sharing this beat
                    break;
                }
                else i++;
            }

            if(speedEvent.rotationValues.Count < laserCount)
            {
                //No left laser event was found on this beat, so randomize this event
                speedEvent.PopulateRotationData(laserCount);
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
        persistentLightProperties = new MaterialPropertyBlock();
        glowProperties = new MaterialPropertyBlock();
    }
}


public class LightEvent : MapElement
{
    public LightEventType Type;
    public LightEventValue Value;
    public float FloatValue = 1f;

    public bool isTransition => Value == LightEventValue.RedTransition || Value == LightEventValue.BlueTransition || Value == LightEventValue.WhiteTransition;


    public LightEvent() {}

    public LightEvent(BeatmapBasicBeatmapEvent beatmapEvent)
    {
        Beat = beatmapEvent.b;
        Type = (LightEventType)beatmapEvent.et;
        Value = (LightEventValue)beatmapEvent.i;
        FloatValue = beatmapEvent.f;
    }
}


public class LaserSpeedEvent : LightEvent
{
    public float rotationSpeed => (int)Value * 20f;
    public List<LaserRotationData> rotationValues;


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
    }


    public void PopulateRotationData(int laserCount)
    {
        rotationValues = new List<LaserRotationData>();
        for(int i = 0; i < laserCount; i++)
        {
            LaserRotationData newData = new LaserRotationData();
            if((int)Value > 0)
            {
                newData.startPosition = UnityEngine.Random.Range(0f, 360f);
                newData.direction = UnityEngine.Random.value >= 0.5f;
            }
            else
            {
                newData.startPosition = 0f;
            }
            rotationValues.Add(newData);
        }
    }
}


public class LightingPropertyEventArgs
{
    public MaterialPropertyBlock laserProperties;
    public MaterialPropertyBlock persistentLaserProperties;
    public MaterialPropertyBlock glowProperties;
    public LightEventType type;
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