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

    private static MaterialPropertyBlock lightProperties;

    public List<LightEvent> backLaserEvents = new List<LightEvent>();
    public List<LightEvent> ringEvents = new List<LightEvent>();
    public List<LightEvent> leftLaserEvents = new List<LightEvent>();
    public List<LightEvent> rightLaserEvents = new List<LightEvent>();
    public List<LightEvent> centerLightEvents = new List<LightEvent>();

    [SerializeField, Range(0f, 1f)] private float lightSaturation;
    [SerializeField, Range(0f, 1f)] private float lightEmissionSaturation;
    [SerializeField] private float lightEmission;


    private void UpdateLightEventType(LightEventType type, List<LightEvent> events, float beat)
    {
        LightEvent last = events.FirstOrDefault(x => x.Beat <= beat);
        LightEventValue value = last?.Value ?? LightEventValue.Off;

        switch(value)
        {
            case LightEventValue.BlueOn:
                SetLightProperties(lightColor2);
                break;
            case LightEventValue.RedOn:
                SetLightProperties(lightColor1);
                break;
            case LightEventValue.Off:
            default:
                SetLightProperties(new Color(0f, 0f, 0f, 0f));
                break;
        }

        LightingPropertyEventArgs eventArgs = new LightingPropertyEventArgs
        {
            properties = lightProperties,
            type = type
        };
        OnLightPropertiesChanged?.Invoke(eventArgs);
    }


    public void UpdateLights(float beat)
    {
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
        lightProperties.SetColor("_BaseColor", GetLightColor(baseColor));
        lightProperties.SetColor("_EmissionColor", GetLightEmission(baseColor));
    }


    private Color GetLightColor(Color baseColor)
    {
        float h, s, v;
        Color.RGBToHSV(baseColor, out h, out s, out v);
        Color newColor = baseColor.SetHSV(h, s * lightSaturation, v);
        newColor.a = baseColor.a;
        return newColor;
    }


    private Color GetLightEmission(Color baseColor)
    {
        float h, s;
        Color.RGBToHSV(baseColor, out h, out s, out _);
        Color newColor = baseColor.SetHSV(h, s * lightEmissionSaturation, lightEmission, true);
        newColor.a = baseColor.a;
        return newColor;
    }


    public void UpdateDifficulty(Difficulty newDifficulty)
    {
        //Temporary fill-in to just make everything static lights for now
        backLaserEvents = new List<LightEvent>()
        {
            new LightEvent
            {
                Beat = 0f,
                Type = LightEventType.BackLasers,
                Value = LightEventValue.RedOn
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
                Value = LightEventValue.BlueOn
            }
        };
        rightLaserEvents = new List<LightEvent>()
        {
            new LightEvent
            {
                Beat = 0f,
                Type = LightEventType.RightRotatingLasers,
                Value = LightEventValue.BlueOn
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

        UpdateLights(TimeManager.CurrentBeat);
    }


    private void Start()
    {
        // TimeManager.OnBeatChanged += UpdateLights;
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateDifficulty;
        ColorManager.OnColorsChanged += (_) => UpdateColors();
    }


    private void Awake()
    {
        lightProperties = new MaterialPropertyBlock();
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
    public float? FloatValue = null;
}


public struct LightingPropertyEventArgs
{
    public MaterialPropertyBlock properties;
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