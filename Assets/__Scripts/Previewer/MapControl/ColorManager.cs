using System;
using System.Linq;
using UnityEngine;

public class ColorManager : MonoBehaviour
{
    private static ColorPalette _currentColors = DefaultColors;
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
        "boostlightcolor1",
        "boostlightcolor2",
        "whitelightcolor",
        "wallcolor"
    };


    public void UpdateSettings(string setting)
    {
        if(setting == "all" || colorSettings.Contains(setting))
        {
            if(SettingsManager.GetBool("coloroverride"))
            {
                CurrentColors = new ColorPalette
                {
                    LeftNoteColor = SettingsManager.GetColor("leftnotecolor"),
                    RightNoteColor = SettingsManager.GetColor("rightnotecolor"),
                    LightColor1 = SettingsManager.GetColor("lightcolor1"),
                    LightColor2 = SettingsManager.GetColor("lightcolor2"),
                    BoostLightColor1 = SettingsManager.GetColor("boostlightcolor1"),
                    BoostLightColor2 = SettingsManager.GetColor("boostlightcolor2"),
                    WhiteLightColor = SettingsManager.GetColor("whitelightcolor"),
                    WallColor = SettingsManager.GetColor("wallcolor")
                };
            }
            else CurrentColors = DefaultColors;
        }
    }


    private void Start()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;
        UpdateSettings("all");
    }


    private static ColorPalette DefaultColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.7843137f, 0.07843138f, 0.07843138f),
        RightNoteColor = new Color(0.1568627f, 0.5568627f, 0.8235294f),
        LightColor1 = new Color(0.85f, 0.085f, 0.085f),
        LightColor2 = new Color(0.1882353f, 0.675294f, 1f),
        BoostLightColor1 = Color.black,
        BoostLightColor2 = Color.black,
        WhiteLightColor = Color.white,
        WallColor = new Color(1f, 0.1882353f, 0.1882353f)
    };
}


public struct ColorPalette
{
    public Color LeftNoteColor;
    public Color RightNoteColor;
    public Color LightColor1;
    public Color LightColor2;
    public Color BoostLightColor1;
    public Color BoostLightColor2;
    public Color WhiteLightColor;
    public Color WallColor;
}