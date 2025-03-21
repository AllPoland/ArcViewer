using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class LightManager : MonoBehaviour
{
    private static bool _staticLights;
    public static bool StaticLights
    {
        get => _staticLights || Scrubbing || EnvironmentManager.CurrentSceneIndex < 0 || EnvironmentManager.Loading || SettingsManager.GetBool("staticlights");
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

    private static float lightGlowBrightness = 1f;

    private static ColorPalette colors => ColorManager.CurrentColors;
    private static Color lightColor1 => BoostActive ? colors.BoostLightColor1 : colors.LightColor1;
    private static Color lightColor2 => BoostActive ? colors.BoostLightColor2 : colors.LightColor2;
    private static Color whiteLightColor => BoostActive ? colors.BoostWhiteLightColor : colors.WhiteLightColor;

    private static readonly string[] lightSettings = new string[]
    {
        "staticlights",
        "lightglowbrightness",
        "chromalightcolors",
        "staticbacklasers",
        "staticringlights",
        "staticleftlasers",
        "staticrightlasers",
        "staticcenterlights"
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

    [SerializeField] private float lightEmission;


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

        LightingPropertyEventArgs eventArgs = new LightingPropertyEventArgs
        {
            sender = this,
            eventList = events,
            lightEvent = lightEvent,
            nextEvent = nextEvent,
            type = type,
            eventIndex = eventIndex,
            laserColor = GetLaserColor(eventColor),
            glowColor = GetLaserGlowColor(eventColor)
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


    public Color GetLaserColor(Color baseColor)
    {
        baseColor.a *= lightEmission;
        return baseColor;
    }


    public Color GetLaserGlowColor(Color baseColor)
    {
        baseColor.a *= lightGlowBrightness;
        return baseColor;
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
        if(lightEvent.CustomColorIdx != null && SettingsManager.GetBool("chromalightcolors"))
        {
            baseColor = LightColorManager.GetColor(lightEvent.CustomColorIdx);
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

        if(nextEvent?.IsTransition ?? false)
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
            t = Easings.EasingFromType(lightEvent.TransitionEasing, t);

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
        int backLaserState = SettingsManager.GetInt("staticbacklasers");
        int ringState = SettingsManager.GetInt("staticringlights");
        int leftLaserState = SettingsManager.GetInt("staticleftlasers");
        int rightLaserState = SettingsManager.GetInt("staticrightlasers");
        int centerLightState = SettingsManager.GetInt("staticcenterlights");

        LightEvent backLasers = new LightEvent
        {
            Beat = 0f,
            Type = LightEventType.BackLasers,
            Value = ValueFromSetting(backLaserState)
        };
        LightEvent rings = new LightEvent
        {
            Beat = 0f,
            Type = LightEventType.Rings,
            Value = ValueFromSetting(ringState)
        };
        LightEvent leftLasers = new LightEvent
        {
            Beat = 0f,
            Type = LightEventType.LeftRotatingLasers,
            Value = ValueFromSetting(leftLaserState)
        };
        LightEvent rightLasers = new LightEvent
        {
            Beat = 0f,
            Type = LightEventType.RightRotatingLasers,
            Value = ValueFromSetting(rightLaserState)
        };
        LightEvent centerLights = new LightEvent
        {
            Beat = 0f,
            Type = LightEventType.CenterLights,
            Value = ValueFromSetting(centerLightState)
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


    private LightEventValue ValueFromSetting(int setting)
    {
        return setting switch
        {
            1 => LightEventValue.RedOn,
            2 => LightEventValue.BlueOn,
            3 => LightEventValue.WhiteOn,
            _ => LightEventValue.Off
        };
    }


    public void UpdateLightParameters()
    {
        lightGlowBrightness = SettingsManager.GetFloat("lightglowbrightness");
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


    private void UpdateDifficulty(Difficulty newDifficulty)
    {
        boostEvents.Clear();

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

        string environmentName = BeatmapManager.EnvironmentName;
        if(newDifficulty.beatmapDifficulty.BasicEvents.Length == 0)
        {
            StaticLights = true;
            return;
        }

        if(SettingsManager.GetBool("skiplights"))
        {
            StaticLights = true;
            return;
        }

        //Avoid loading lightshows with too many events (to avoid crashes)
        int maxEvents = SettingsManager.GetInt("maxlightevents");
        if(maxEvents > 0 && newDifficulty.beatmapDifficulty.BasicEvents.Length > maxEvents)
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Notification, "Disabled the lightshow because it has too many events.");
            StaticLights = true;
            return;
        }

        if(EnvironmentManager.V3Environments.Contains(environmentName))
        {
            StaticLights = true;
            return;
        }

        StaticLights = false;
        FlipBackLasers = backLaserFlipEnvironments.Contains(environmentName);

        foreach(BeatmapColorBoostBeatmapEvent beatmapBoostEvent in newDifficulty.beatmapDifficulty.BoostEvents)
        {
            boostEvents.Add(new BoostEvent(beatmapBoostEvent));
        }
        boostEvents.SortElementsByBeat();

        foreach(BeatmapBasicBeatmapEvent beatmapEvent in newDifficulty.beatmapDifficulty.BasicEvents)
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

        if(EnvironmentManager.CurrentSceneIndex >= 0 && !EnvironmentManager.Loading)
        {
            PopulateLaserRotationEventData();
            RingManager.PopulateRingEventData();
        }

        UpdateLights(TimeManager.CurrentBeat);
    }


    private void UpdateEnvironment(int newEnvironmentIndex)
    {
        //Recalculate ring and laser movement for the new environment
        PopulateLaserRotationEventData();
        RingManager.PopulateRingEventData();

        UpdateLightParameters();
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
                EnvironmentLightParameters envParams = EnvironmentManager.DefaultEnvironmentParameters;
                //Account for name filters for rotation events that only effect big/small rings
                if(string.IsNullOrEmpty(beatmapEvent.customData?.nameFilter) || envParams.SmallRingNameFilters.Contains(beatmapEvent.customData.nameFilter))
                {
                    RingManager.SmallRingRotationEvents.Add(new RingRotationEvent(beatmapEvent, false));
                }
                if(string.IsNullOrEmpty(beatmapEvent.customData?.nameFilter) || envParams.BigRingNameFilters.Contains(beatmapEvent.customData.nameFilter))
                {
                    RingManager.BigRingRotationEvents.Add(new RingRotationEvent(beatmapEvent, true));
                }
                break;
            case LightEventType.RingZoom:
                RingManager.RingZoomEvents.Add(new RingZoomEvent(beatmapEvent));
                break;
        }
    }


    private void PopulateLaserRotationEventData()
    {
        int laserCount = EnvironmentManager.CurrentEnvironmentParameters.RotatingLaserCount;
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

        EnvironmentManager.OnEnvironmentUpdated += UpdateEnvironment;

        SettingsManager.OnSettingsUpdated += UpdateSettings;
        ColorManager.OnColorsChanged += (_) => UpdateLightParameters();

        OnStaticLightsChanged += UpdateLightParameters;
        StaticLights = true;
    }
}