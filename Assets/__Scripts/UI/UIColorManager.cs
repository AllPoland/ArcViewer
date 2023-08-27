using System;
using System.Collections;
using UnityEngine;

public class UIColorManager : MonoBehaviour
{
    public static UIColorManager Instance { get; private set; }

    private static UIColorPalette _colorPalette;
    public static UIColorPalette ColorPalette
    {
        get => _colorPalette;
        private set
        {
            _colorPalette = value;
            OnColorPaletteChanged?.Invoke(_colorPalette);
        }
    }

    public static event Action<UIColorPalette> OnColorPaletteChanged;

    [field:SerializeField] public Color PreviewModeColor { get; private set; }
    [field:SerializeField] public Color ReplayModeColor { get; private set; }
    [field:SerializeField] public Color SoupColor { get; private set; }

    [field:SerializeField] public float BackgroundBrightness { get; private set; }
    [field:SerializeField] public float DarkBackgroundBrightness { get; private set; }
    [field:SerializeField] public float TransparentBackgroundOpacity { get; private set; }

    [Space]
    [SerializeField] private float transitionTime = 0.5f;

    private bool initializedSettings = false;
    private Coroutine colorChangeCoroutine;


    public IEnumerator SetUIColorCoroutine(Color newColor)
    {
        Color previousColor = ColorPalette.standardColor;

        float t = 0f;
        while(t < 1f)
        {
            Color currentColor = Color.Lerp(previousColor, newColor, t);
            ColorPalette = new UIColorPalette(currentColor);

            t += Time.deltaTime / transitionTime;
            yield return null;
        }

        ColorPalette = new UIColorPalette(newColor);
    }


    public static void SetUIColor(Color newColor, bool animate = true)
    {
        if(Instance.colorChangeCoroutine != null)
        {
            Instance.StopCoroutine(Instance.colorChangeCoroutine);
        }

        if(animate)
        {
            Instance.colorChangeCoroutine = Instance.StartCoroutine(Instance.SetUIColorCoroutine(newColor));
        }
        else
        {
            ColorPalette = new UIColorPalette(newColor);
        }
    }


    private void UpdateSettings(string setting)
    {
        if(setting == "all" || setting == "replaymode" || setting == "useuicolor" || setting == "uicolor" || setting == TheSoup.Rule)
        {
            if(SettingsManager.GetBool("useuicolor"))
            {
                bool animate = (setting == "useuicolor" || setting == "all") && initializedSettings;
                SetUIColor(SettingsManager.GetColor("uicolor"), animate);
            }
            else if(SettingsManager.GetBool(TheSoup.Rule))
            {
                SetUIColor(SoupColor, initializedSettings);
            }
            else if(ReplayManager.IsReplayMode || (UIStateManager.CurrentState != UIState.Previewer && SettingsManager.GetBool("replaymode")))
            {
                SetUIColor(ReplayModeColor, initializedSettings);
            }
            else SetUIColor(PreviewModeColor, initializedSettings);

            initializedSettings = true;
        }
    }


    private void UpdateReplayMode(bool replayMode)
    {
        if(SettingsManager.Loaded)
        {
            UpdateSettings("all");
        }
        else if(replayMode)
        {
            SetUIColor(ReplayModeColor);
        }
        else SetUIColor(PreviewModeColor);
    }


    private void UpdateUIState(UIState newState)
    {
        if(newState == UIState.Previewer)
        {
            UpdateSettings("all");
        }
    }


    private void Start()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;
        UIStateManager.OnUIStateChanged += UpdateUIState;
        ReplayManager.OnReplayModeChanged += UpdateReplayMode;

        if(SettingsManager.Loaded)
        {
            UpdateSettings("all");
        }
    }


    private void OnEnable()
    {
        if(Instance && Instance != this)
        {
            Debug.LogWarning("Multiple UIColorManagers in the scene!");
            this.enabled = false;
        }
        else
        {
            Instance = this;
        }
    }
    

    private void OnDisable()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }
}


public class UIColorPalette
{
    public Color standardColor;
    public Color backgroundColor;
    public Color background2Color;
    public Color transparentBackgroundColor;


    public UIColorPalette(Color baseColor)
    {
        standardColor = baseColor;
        backgroundColor = baseColor * UIColorManager.Instance.BackgroundBrightness;
        background2Color = baseColor * UIColorManager.Instance.DarkBackgroundBrightness;

        transparentBackgroundColor = backgroundColor;
        transparentBackgroundColor.a = UIColorManager.Instance.TransparentBackgroundOpacity;

        backgroundColor.a = 1f;
        background2Color.a = 1f;
    }
}