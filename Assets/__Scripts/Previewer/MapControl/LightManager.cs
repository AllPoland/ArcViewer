using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightManager : MonoBehaviour
{
    public static bool BoostActive;

    public static event Action<LightingPropertyEventArgs> OnLightPropertiesChanged;

    private static ColorPalette colors => ColorManager.CurrentColors;
    private static Color lightColor1 => BoostActive ? colors.BoostLightColor1 : colors.LightColor1;
    private static  Color lightColor2 => BoostActive ? colors.BoostLightColor2 : colors.LightColor2;
    private static Color whiteLightColor => BoostActive ? colors.BoostWhiteLightColor : colors.WhiteLightColor;

    private static BeatmapColorBoostBeatmapEvent[] boostEvents => BeatmapManager.CurrentDifficulty.beatmapDifficulty.colorBoostBeatMapEvents;

    private static MaterialPropertyBlock lightProperties;
    private static MaterialPropertyBlock glowProperties;

    public List<LightEvent> backLaserEvents = new List<LightEvent>()
    {
        new LightEvent
        {
            Beat = 0f,
            Type = LightEventType.BackLasers,
            Value = LightEventValue.RedOn
        }
    };
    public List<LightEvent> ringEvents = new List<LightEvent>()
    {
        new LightEvent
        {
            Beat = 0f,
            Type = LightEventType.Rings,
            Value = LightEventValue.BlueOn
        }
    };
    public List<LightEvent> leftLaserEvents = new List<LightEvent>()
    {
        new LightEvent
        {
            Beat = 0f,
            Type = LightEventType.LeftRotatingLasers,
            Value = LightEventValue.BlueOn
        }
    };
    public List<LightEvent> rightLaserEvents = new List<LightEvent>()
    {
        new LightEvent
        {
            Beat = 0f,
            Type = LightEventType.RightRotatingLasers,
            Value = LightEventValue.BlueOn
        }
    };
    public List<LightEvent> centerLightEvents = new List<LightEvent>()
    {
        new LightEvent
        {
            Beat = 0f,
            Type = LightEventType.CenterLights,
            Value = LightEventValue.BlueOn
        }
    };

    [SerializeField, Range(0f, 1f)] private float lightSaturation;
    [SerializeField, Range(0f, 1f)] private float lightEmissionSaturation;
    [SerializeField] private float lightEmission;


    private void UpdateLightEventType(LightEventType type, List<LightEvent> events, float beat)
    {
        LightEvent last = events.LastOrDefault(x => x.Beat <= beat);
        LightEventValue value = last?.Value ?? LightEventValue.Off;

        Color baseColor = GetEventColor(value, last);
        SetLightProperties(baseColor);

        LightingPropertyEventArgs eventArgs = new LightingPropertyEventArgs
        {
            laserProperties = lightProperties,
            glowProperties = glowProperties,
            emission = lightEmission,
            type = type
        };
        OnLightPropertiesChanged?.Invoke(eventArgs);
    }


    private Color GetEventColor(LightEventValue value, LightEvent lightEvent)
    {
        switch(value)
        {
            case LightEventValue.RedOn:
                return lightColor1;
            case LightEventValue.RedFlash:
                //We can be sure lightEvent isn't null because value would be set to Off
                return GetFlashColor(lightEvent, lightColor1);
            case LightEventValue.RedFade:
                return GetFadeColor(lightEvent, lightColor1);

            case LightEventValue.BlueOn:
                return lightColor2;
            case LightEventValue.BlueFlash:
                return GetFlashColor(lightEvent, lightColor2);
            case LightEventValue.BlueFade:
                return GetFadeColor(lightEvent, lightColor2);

            case LightEventValue.WhiteOn:
                return whiteLightColor;
            case LightEventValue.WhiteFlash:
                return GetFlashColor(lightEvent, whiteLightColor);
            case LightEventValue.WhiteFade:
                return GetFadeColor(lightEvent, whiteLightColor);

            case LightEventValue.Off:
            default:
                return new Color(0f, 0f, 0f, 0f);
        }
    }


    private Color GetFlashColor(LightEvent lightEvent, Color baseColor)
    {
        const float flashIntensity = 1.3f;
        const float flashTime = 0.1f;
        const float flashDecayTime = 0.2f;

        float timeDifference = TimeManager.CurrentTime - lightEvent.Time;
        if(timeDifference >= flashTime + flashDecayTime)
        {
            baseColor.a = 1f;
        }
        else if(timeDifference >= flashTime)
        {
            float t = (timeDifference - flashTime) / flashDecayTime;
            baseColor.a = Mathf.Lerp(flashIntensity, 1f, Easings.Quad.InOut(t));
        }
        else if(timeDifference >= 0)
        {
            float t = timeDifference / flashTime;
            baseColor.a = Mathf.Lerp(1f, flashIntensity, Easings.Quad.InOut(t));
        }
        else baseColor.a = 0f;

        return baseColor;
    }


    private Color GetFadeColor(LightEvent lightEvent, Color baseColor)
    {
        const float flashIntensity = 1.2f;
        const float fadeTime = 0.4f;

        float timeDifference = TimeManager.CurrentTime - lightEvent.Time;
        if(timeDifference >= fadeTime)
        {
            baseColor.a = 0f;
        }
        else if(timeDifference >= 0)
        {
            float t = timeDifference / fadeTime;
            baseColor.a = Mathf.Lerp(flashIntensity, fadeTime, Easings.Quad.InOut(t));
        }
        else baseColor.a = 0f;

        return baseColor;
    }


    public void UpdateLights(float beat)
    {
        BoostActive = boostEvents.LastOrDefault(x => x.b <= beat)?.o ?? false;

        UpdateLightEventType(LightEventType.BackLasers, backLaserEvents, beat);
        UpdateLightEventType(LightEventType.Rings, ringEvents, beat);
        UpdateLightEventType(LightEventType.LeftRotatingLasers, leftLaserEvents, beat);
        UpdateLightEventType(LightEventType.RightRotatingLasers, rightLaserEvents, beat);
        UpdateLightEventType(LightEventType.CenterLights, centerLightEvents, beat);
    }


    public void UpdateColors()
    {
        UpdateLights(TimeManager.CurrentBeat);
    }


    private void SetLightProperties(Color baseColor)
    {
        glowProperties.SetColor("_BaseColor", baseColor);
        lightProperties.SetColor("_BaseColor", GetLightColor(baseColor));
        lightProperties.SetColor("_EmissionColor", GetLightEmission(baseColor));
    }


    private Color GetLightColor(Color baseColor)
    {
        float h, s, v;
        Color.RGBToHSV(baseColor, out h, out s, out v);
        Color newColor = baseColor.SetHSV(h, s * lightSaturation, v);
        newColor.a = Mathf.Clamp(baseColor.a, 0f, 1f);
        return newColor;
    }


    private Color GetLightEmission(Color baseColor)
    {
        float h, s;
        Color.RGBToHSV(baseColor, out h, out s, out _);
        Color newColor = baseColor.SetHSV(h, s * lightEmissionSaturation, lightEmission * baseColor.a, true);
        newColor.a = Mathf.Clamp(baseColor.a, 0f, 1f);
        return newColor;
    }


    public void UpdateDifficulty(Difficulty newDifficulty)
    {
        if(newDifficulty.beatmapDifficulty.basicBeatMapEvents.Length == 0)
        {
            SetStaticLayout();
            UpdateLights(TimeManager.CurrentBeat);
            return;
        }

        backLaserEvents.Clear();
        ringEvents.Clear();
        leftLaserEvents.Clear();
        rightLaserEvents.Clear();
        centerLightEvents.Clear();

        foreach(BeatmapBasicBeatmapEvent beatmapEvent in newDifficulty.beatmapDifficulty.basicBeatMapEvents)
        {
            AddLightEvent(beatmapEvent);
        }

        UpdateLights(TimeManager.CurrentBeat);
    }


    private void AddLightEvent(BeatmapBasicBeatmapEvent beatmapEvent)
    {
        LightEvent newEvent = LightEvent.LightEventFromBasicBeatmapEvent(beatmapEvent);
        switch(newEvent.Type)
        {
            case LightEventType.BackLasers:
                backLaserEvents.Add(newEvent);
                break;
            case LightEventType.Rings:
                ringEvents.Add(newEvent);
                break;
            case LightEventType.LeftRotatingLasers:
                leftLaserEvents.Add(newEvent);
                break;
            case LightEventType.RightRotatingLasers:
                rightLaserEvents.Add(newEvent);
                break;
            case LightEventType.CenterLights:
                centerLightEvents.Add(newEvent);
                break;
        }
    }


    private void SetStaticLayout()
    {
        backLaserEvents = new List<LightEvent>()
        {
            new LightEvent
            {
                Beat = 0f,
                Type = LightEventType.BackLasers,
                Value = LightEventValue.BlueOn
            }
        };
        ringEvents = new List<LightEvent>()
        {
            new LightEvent
            {
                Beat = 0f,
                Type = LightEventType.Rings,
                Value = LightEventValue.BlueOn
            }
        };
        leftLaserEvents = new List<LightEvent>()
        {
            new LightEvent
            {
                Beat = 0f,
                Type = LightEventType.LeftRotatingLasers,
                Value = LightEventValue.Off
            }
        };
        rightLaserEvents = new List<LightEvent>()
        {
            new LightEvent
            {
                Beat = 0f,
                Type = LightEventType.RightRotatingLasers,
                Value = LightEventValue.Off
            }
        };
        centerLightEvents = new List<LightEvent>()
        {
            new LightEvent
            {
                Beat = 0f,
                Type = LightEventType.CenterLights,
                Value = LightEventValue.BlueOn
            }
        };
    }


    private void Start()
    {
        TimeManager.OnBeatChanged += UpdateLights;
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateDifficulty;
        ColorManager.OnColorsChanged += (_) => UpdateColors();
    }


    private void Awake()
    {
        lightProperties = new MaterialPropertyBlock();
        glowProperties = new MaterialPropertyBlock();

        SetStaticLayout();
    }
}


public class LightEvent
{
    private float _beat;
    public float Beat
    {
        get => _beat;
        set
        {
            _beat = value;
            Time = TimeManager.TimeFromBeat(_beat);
        }
    }
    public float Time { get; private set; }

    public LightEventType Type;
    public LightEventValue Value;
    public float FloatValue;


    public static LightEvent LightEventFromBasicBeatmapEvent(BeatmapBasicBeatmapEvent beatmapEvent)
    {
        return new LightEvent
        {
            Beat = beatmapEvent.b,
            Type = (LightEventType)beatmapEvent.et,
            Value = (LightEventValue)beatmapEvent.i,
            FloatValue = beatmapEvent.f
        };
    }
}


public struct LightingPropertyEventArgs
{
    public MaterialPropertyBlock laserProperties;
    public MaterialPropertyBlock glowProperties;
    public float emission;
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