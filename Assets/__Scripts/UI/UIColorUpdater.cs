using UnityEngine;
using UnityEngine.UI;

public class UIColorUpdater : MonoBehaviour
{
    [SerializeField] private Graphic graphic;
    [SerializeField] private GraphicType graphicType;


    private void UpdateColors(UIColorPalette palette)
    {
        if(!graphic)
        {
            Debug.LogWarning($"UI object: {this} has missing color swap graphic!");
            return;
        }

        switch(graphicType)
        {
            default:
            case GraphicType.Standard:
                graphic.color = palette.standardColor;
                break;
            case GraphicType.Background:
                graphic.color = palette.backgroundColor;
                break;
            case GraphicType.TransparentBackground:
                graphic.color = palette.transparentBackgroundColor;
                break;
            case GraphicType.DarkBackground:
                graphic.color = palette.background2Color;
                break;
        }
    }


    private void OnEnable()
    {
        UIColorManager.OnColorPaletteChanged += UpdateColors;
        if(UIColorManager.ColorPalette != null)
        {
            UpdateColors(UIColorManager.ColorPalette);
        }
    }


    private void OnDisable()
    {
        UIColorManager.OnColorPaletteChanged -= UpdateColors;
    }


    private enum GraphicType
    {
        Standard,
        Background,
        TransparentBackground,
        DarkBackground
    }
}