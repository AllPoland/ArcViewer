using System;
using System.Linq;
using UnityEngine;

public class ColorManager : MonoBehaviour
{
    private static ColorPalette _currentColors;
    public static ColorPalette CurrentColors
    {
        get => _currentColors;
        set
        {
            _currentColors = value;
            OnColorsChanged?.Invoke(_currentColors);
        }
    }

    public static event Action<ColorPalette> OnColorsChanged;

    private static readonly string[] colorSettings =
    {
        "songcorecolors",
        "coloroverride",
        "leftnotecolor",
        "rightnotecolor",
        "lightcolor1",
        "lightcolor2",
        "whitelightcolor",
        "boostlightcolor1",
        "boostlightcolor2",
        "boostwhitelightcolor",
        "wallcolor"
    };

    private static NullableColorPalette SongCoreColors;


    private void UpdateCurrentColors()
    {
        ColorPalette newColors;
        if(SettingsManager.GetBool("coloroverride"))
        {
            newColors = new ColorPalette
            {
                LeftNoteColor = SettingsManager.GetColor("leftnotecolor"),
                RightNoteColor = SettingsManager.GetColor("rightnotecolor"),
                LightColor1 = SettingsManager.GetColor("lightcolor1"),
                LightColor2 = SettingsManager.GetColor("lightcolor2"),
                WhiteLightColor = SettingsManager.GetColor("whitelightcolor"),
                BoostLightColor1 = SettingsManager.GetColor("boostlightcolor1"),
                BoostLightColor2 = SettingsManager.GetColor("boostlightcolor2"),
                BoostWhiteLightColor = SettingsManager.GetColor("boostwhitelightcolor"),
                WallColor = SettingsManager.GetColor("wallcolor")
            };
        }
        else newColors = DefaultColors;

        if(SettingsManager.GetBool("songcorecolors") && SongCoreColors != null)
        {
            newColors.StackPalette(SongCoreColors);
        }

        CurrentColors = newColors;
    }


    public void UpdateDifficulty(Difficulty newDifficulty)
    {
        SongCoreColors = newDifficulty.colors;
        UpdateCurrentColors();
    }


    public void UpdateSettings(string setting)
    {
        if(setting == "all" || colorSettings.Contains(setting))
        {
            UpdateCurrentColors();
        }
    }


    private void Awake()
    {
        CurrentColors = DefaultColors;
    }


    private void Start()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateDifficulty;

        UpdateSettings("all");
    }


    public static ColorPalette DefaultColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.7843137f, 0.07843138f, 0.07843138f),
        RightNoteColor = new Color(0.1568627f, 0.5568627f, 0.8235294f),
        LightColor1 = new Color(0.85f, 0.085f, 0.085f),
        LightColor2 = new Color(0.1882353f, 0.675294f, 1f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.85f, 0.085f, 0.085f),
        BoostLightColor2 = new Color(0.1882353f, 0.675294f, 1f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(1f, 0.1882353f, 0.1882353f)
    };
}


public class ColorPalette
{
    public Color LeftNoteColor;
    public Color RightNoteColor;
    public Color LightColor1;
    public Color LightColor2;
    public Color WhiteLightColor;
    public Color BoostLightColor1;
    public Color BoostLightColor2;
    public Color BoostWhiteLightColor;
    public Color WallColor;


    public void StackPalette(NullableColorPalette toAdd)
    {
        if(toAdd == null)
        {
            return;
        }

        LeftNoteColor = toAdd.LeftNoteColor ?? LeftNoteColor;
        RightNoteColor = toAdd.RightNoteColor ?? RightNoteColor;
        LightColor1 = toAdd.LightColor1 ?? LightColor1;
        LightColor2 = toAdd.LightColor2 ?? LightColor2;
        WhiteLightColor = toAdd.WhiteLightColor ?? WhiteLightColor;
        BoostLightColor1 = toAdd.BoostLightColor1 ?? BoostLightColor1;
        BoostLightColor2 = toAdd.BoostLightColor2 ?? BoostLightColor2;
        BoostWhiteLightColor = toAdd.BoostWhiteLightColor ?? BoostWhiteLightColor;
        WallColor = toAdd.WallColor ?? WallColor;
    }
}


public class NullableColorPalette
{
    public Color? LeftNoteColor;
    public Color? RightNoteColor;
    public Color? LightColor1;
    public Color? LightColor2;
    public Color? WhiteLightColor;
    public Color? BoostLightColor1;
    public Color? BoostLightColor2;
    public Color? BoostWhiteLightColor;
    public Color? WallColor;
}