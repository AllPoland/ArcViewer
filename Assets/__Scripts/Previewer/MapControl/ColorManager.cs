using System;
using System.Collections;
using System.Collections.Generic;
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


public class ColorPalette
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