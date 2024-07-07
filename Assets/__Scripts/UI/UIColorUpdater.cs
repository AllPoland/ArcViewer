using UnityEngine;
using UnityEngine.UI;

public class UIColorUpdater : MonoBehaviour
{
    [SerializeField] private Graphic graphic;
    [SerializeField] private UIColorType colorType;


    public void SetColorType(UIColorType type)
    {
        colorType = type;
        UpdateColors(UIColorManager.ColorPalette);
    }


    private void UpdateColors(UIColorPalette palette)
    {
        if(!graphic)
        {
            Debug.LogWarning($"UI object: {this} has missing color swap graphic!");
            return;
        }

        if(palette == null)
        {
            return;
        }

        switch(colorType)
        {
            default:
            case UIColorType.Standard:
                graphic.color = palette.standardColor;
                break;
            case UIColorType.Background:
                graphic.color = palette.backgroundColor;
                break;
            case UIColorType.TransparentBackground:
                graphic.color = palette.transparentBackgroundColor;
                break;
            case UIColorType.DarkBackground:
                graphic.color = palette.background2Color;
                break;
        }
    }


    private void OnEnable()
    {
        UIColorManager.OnColorPaletteChanged += UpdateColors;
        UpdateColors(UIColorManager.ColorPalette);
    }


    private void OnDisable()
    {
        UIColorManager.OnColorPaletteChanged -= UpdateColors;
    }
}