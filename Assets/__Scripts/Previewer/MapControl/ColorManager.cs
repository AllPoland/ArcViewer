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
        "chromaobjectcolors",
        "songcorecolors",
        "environmentcolors",
        "difficultycolors",
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

    private static NullableColorPalette difficultyColorScheme;
    private static NullableColorPalette difficultySongCoreColors;


    private void UpdateCurrentColors()
    {
        ColorPalette newColors;
        if(SettingsManager.GetBool("coloroverride"))
        {
            //Use custom user set colors as the base palette, ignore all vanilla colors
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
        else 
        {
            //Use the vanilla color scheme for the difficulty
            if(SettingsManager.GetBool("environmentcolors"))
            {
                newColors = GetEnvironmentColors(BeatmapManager.EnvironmentName);
            }
            else newColors = DefaultColors;

            if(SettingsManager.GetBool("difficultycolors"))
            {
                //Stack the difficulty-specific color scheme
                newColors.StackPalette(difficultyColorScheme);
            }
        }

        if(SettingsManager.GetBool("songcorecolors"))
        {
            //SongCore color overrides replace everything
            newColors.StackPalette(difficultySongCoreColors);
        }

        CurrentColors = newColors;
    }


    private static ColorPalette GetEnvironmentColors(string environmentName)
    {
        //Oh god oh fuck
        switch(environmentName)
        {
            default:
            case "DefaultEnvironment":
            case "TriangleEnvironment":
            case "NiceEnvironment":
            case "BigMirrorEnvironment":
            case "DragonsEnvironment":
            case "MonstercatEnvironment":
            case "PanicEnvironment":
                return DefaultColors;
            case "OriginsEnvironment":
                return OriginsColors;
            case "KDAEnvironment":
                return KdaColors;
            case "CrabRaveEnvironment":
                return CrabRaveColors;
            case "RocketEnvironment":
                return RocketColors;
            case "GreenDayEnvironment":
            case "GreenDayGrenadeEnvironment":
                return GreenDayColors;
            case "TimbalandEnvironment":
                return TimbalandColors;
            case "FitBeatEnvironment":
                return FitBeatColors;
            case "LinkinParkEnvironment":
                return LinkinParkColors;
            case "BTSEnvironment":
                return BtsColors;
            case "KaleidoscopeEnvironment":
                return KaleidoscopeColors;
            case "InterscopeEnvironment":
                return InterscopeColors;
            case "SkrillexEnvironment":
                return SkrillexColors;
            case "BillieEnvironment":
                return BillieColors;
            case "HalloweenEnvironment":
                return SpookyColors;
            case "GagaEnvironment":
                return GagaColors;
            case "WeaveEnvironment":
                ColorPalette weaveColors = DefaultColors;
                weaveColors.BoostLightColor1 = new Color(0.8218409f, 0.08627451f, 0.8509804f);
                weaveColors.BoostLightColor2 = new Color(0.5320754f, 0.5320754f, 0.5320754f);
                return weaveColors;
            case "PyroEnvironment":
                return PyroColors;
            case "EDMEnvironment":
                return EdmColors;
            case "TheSecondEnvironment":
                ColorPalette theSecondColors = DefaultColors;
                theSecondColors.BoostLightColor1 = new Color(0.8235294f, 0.08627451f, 0.8509804f);
                theSecondColors.BoostLightColor2 = new Color(0f, 1f, 0.6478302f);
                return theSecondColors;
            case "LizzoEnvironment":
                return LizzoColors;
            case "TheWeekndEnvironment":
                return WeekndColors;
            case "RockMixtapeEnvironment":
                return RockMixtapeColors;
            case "Dragons2Environment":
                return Dragons2Colors;
            case "Panic2Environment":
                return Panic2Colors;
            case "QueenEnvironment":
                return QueenColors;
        }
    }


    public static Color ColorFromCustomDataColor(float[] customColor)
    {
        Color newColor = Color.black;
        for(int i = 0; i < customColor.Length; i++)
        {
            //Loop only through present rgba values and ignore missing ones
            //a will default to 1 if missing because the color is initialized to black
            switch(i)
            {
                case 0:
                    newColor.r = customColor[i];
                    break;
                case 1:
                    newColor.g = customColor[i];
                    break;
                case 2:
                    newColor.b = customColor[i];
                    break;
                case 3:
                    newColor.a = customColor[i];
                    break;
                default:
                    //For some reason there are more than 4 elements - we'll ignore these
                    return newColor;
            }
        }
        return newColor;
    }


    public void UpdateDifficulty(Difficulty newDifficulty)
    {
        difficultyColorScheme = newDifficulty.colorScheme;
        difficultySongCoreColors = newDifficulty.songCoreColors;
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

    public static ColorPalette OriginsColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.6792453f, 0.5712628f, 0f),
        RightNoteColor = new Color(0.7075472f, 0f, 0.5364411f),
        LightColor1 = new Color(0.4910995f, 0.6862745f, 0.7f),
        LightColor2 = new Color(0.03844783f, 0.6862745f, 0.9056604f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.4910995f, 0.6862745f, 0.7f),
        BoostLightColor2 = new Color(0.03844783f, 0.6862745f, 0.9056604f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.06167676f, 0.2869513f, 0.3962264f)
    };

    public static ColorPalette KdaColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.6588235f, 0.2627451f, 0.1607843f),
        RightNoteColor = new Color(0.5019608f, 0.08235294f,  0.572549f),
        LightColor1 = new Color(1f, 0.3960785f, 0.2431373f),
        LightColor2 = new Color(0.7607844f, 0.1254902f, 0.8666667f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(1f, 0.3960785f, 0.2431373f),
        BoostLightColor2 = new Color(0.7607844f, 0.1254902f, 0.8666667f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(1f, 0.3960785f, 0.2431373f)
    };

    public static ColorPalette CrabRaveColors => new ColorPalette
    {
        LeftNoteColor = new Color(0f, 0.7130001f, 0.07806564f),
        RightNoteColor = new Color(0.04805952f, 0.5068096f, 0.734f),
        LightColor1 = new Color(0.134568f, 0.756f, 0.1557533f),
        LightColor2 = new Color(0.05647058f, 0.6211764f, 0.9f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.134568f, 0.756f, 0.1557533f),
        BoostLightColor2 = new Color(0.05647058f, 0.6211764f, 0.9f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0f, 0.8117648f, 0.09019608f)
    };

    public static ColorPalette RocketColors => new ColorPalette
    {
        LeftNoteColor = new Color(1f, 0.4980392f, 0f),
        RightNoteColor = new Color(0f, 0.5294118f, 1f),
        LightColor1 = new Color(0.9f, 0.4866279f, 0.3244186f),
        LightColor2 = new Color(0.4f, 0.7180724f, 1f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.9f, 0.4866279f, 0.3244186f),
        BoostLightColor2 = new Color(0.4f, 0.7180724f, 1f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.3176471f, 0.6117647f, 0.7254902f)
    };

    public static ColorPalette GreenDayColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.2588235f, 0.7843138f, 0.01960784f),
        RightNoteColor = new Color(0f, 0.7137255f, 0.6705883f),
        LightColor1 = new Color(0f, 0.7137255f, 0.6705883f),
        LightColor2 = new Color(0.2588235f, 0.7843137f, 0.01960784f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0f, 0.7137255f, 0.6705883f),
        BoostLightColor2 = new Color(0.2588235f, 0.7843137f, 0.01960784f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0f, 0.8117648f, 0.09019608f)
    };

    public static ColorPalette TimbalandColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.5019608f, 0.5019608f, 0.5019608f),
        RightNoteColor = new Color(0.1f, 0.5517647f, 1f),
        LightColor1 = new Color(0.1f, 0.5517647f, 1f),
        LightColor2 = new Color(0.1f, 0.5517647f, 1f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.1f, 0.5517647f, 1f),
        BoostLightColor2 = new Color(0.1f, 0.5517647f, 1f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.5019608f, 0.5019608f, 0.5019608f)
    };

    public static ColorPalette FitBeatColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.8000001f, 0.6078432f, 0.1568628f),
        RightNoteColor = new Color(0.7921569f, 0.1607843f, 0.682353f),
        LightColor1 = new Color(0.8f, 0.5594772f, 0.5594772f),
        LightColor2 = new Color(0.5594772f, 0.5594772f, 0.8f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.8f, 0.5594772f, 0.5594772f),
        BoostLightColor2 = new Color(0.5594772f, 0.5594772f, 0.8f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.2784314f, 0.2784314f, 0.4f)
    };

    public static ColorPalette LinkinParkColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.6627451f, 0.1643608f, 0.1690187f),
        RightNoteColor = new Color(0.3870196f, 0.5168997f, 0.5568628f),
        LightColor1 = new Color(0.7529412f, 0.672753f, 0.5925647f),
        LightColor2 = new Color(0.6241197f, 0.6890281f, 0.709f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.922f, 0.5957885f, 0.255394f),
        BoostLightColor2 = new Color(0.282353f, 0.4586275f, 0.6235294f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.6627451f, 0.1647059f, 0.172549f)
    };

    public static ColorPalette BtsColors => new ColorPalette
    {
        LeftNoteColor = new Color(1f, 0.09019607f, 0.4059771f),
        RightNoteColor = new Color(0.8018868f, 0f, 0.7517689f),
        LightColor1 = new Color(0.7843137f, 0.1254902f, 0.5010797f),
        LightColor2 = new Color(0.6941177f, 0.1254902f, 0.8666667f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.9019608f, 0.5411765f, 1f),
        BoostLightColor2 = new Color(0.3490196f, 0.8078431f, 1f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.6698113f, 0.1800908f, 0.5528399f),
    };

    public static ColorPalette KaleidoscopeColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.65882355f, 0.1254902f, 0.1254902f),
        RightNoteColor = new Color(0.28235295f, 0.28235295f, 0.28235295f),
        LightColor1 = new Color(0.65882355f, 0.1254902f, 0.1254902f),
        LightColor2 = new Color(0.47058824f, 0.47058824f, 0.47058824f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.50196081f, 0f, 0f),
        BoostLightColor2 = new Color(0.49244517f, 0f, 0.53725493f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.25098041f, 0.25098041f, 0.25098041f)
    };

    public static ColorPalette InterscopeColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.726415f, 0.62691f, 0.31181f),
        RightNoteColor = new Color(0.589571f, 0.297888f, 0.723f),
        LightColor1 = new Color(0.724254f, 0.319804f, 0.913725f),
        LightColor2 = new Color(0.764706f, 0.758971f, 0.913725f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.792453f, 0.429686f, 0.429868f),
        BoostLightColor2 = new Color(0.7038f, 0.715745f, 0.765f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.588235f, 0.298039f, 0.721569f)
    };

    public static ColorPalette SkrillexColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.69803923f, 0.14117648f, 0.36862746f),
        RightNoteColor = new Color(0.32933334f, 0.32299998f, 0.38f),
        LightColor1 = new Color(0.80000001f, 0.28000003f, 0.58594489f),
        LightColor2 = new Color(0.06525807f, 0.57800001f, 0.56867743f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.81176478f, 0.30588236f, 0.30588236f),
        BoostLightColor2 = new Color(0.27843139f, 0.80000001f, 0.44597632f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.15686275f, 0.60392159f, 0.60392159f)
    };

    public static ColorPalette BillieColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.80000001f, 0.64481932f, 0.43200001f),
        RightNoteColor = new Color(0.54808509f, 0.61276591f, 0.63999999f),
        LightColor1 = new Color(0.81960785f, 0.442f, 0.184f),
        LightColor2 = new Color(0.94117647f, 0.70677096f, 0.56470591f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.80000001f, 0f, 0f),
        BoostLightColor2 = new Color(0.55686277f, 0.7019608f, 0.77647066f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.71325314f, 0.56140977f, 0.78301889f)
    };

    public static ColorPalette SpookyColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.81960785f, 0.49807876f, 0.27702752f),
        RightNoteColor = new Color(0.37894738f, 0.35789475f, 0.40000001f),
        LightColor1 = new Color(0.90196079f, 0.23009226f, 0f),
        LightColor2 = new Color(0.46005884f, 0.56889427f, 0.92941177f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.33768433f, 0.63207543f, 0.33690813f),
        BoostLightColor2 = new Color(0.60209066f, 0.3280082f, 0.85849059f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.81960791f, 0.44313729f, 0.18431373f)
    };

    public static ColorPalette GagaColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.85f, 0.4333333f, 0.7833334f),
        RightNoteColor = new Color(0.4705882f, 0.8f, 0.4078431f),
        LightColor1 = new Color(0.706f, 0.649f, 0.2394706f),
        LightColor2 = new Color(0.894f, 0.1625455f, 0.7485644f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.754717f, 0.3610244f, 0.22071921f),
        BoostLightColor2 = new Color(0f, 0.7058824f, 1f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.9921569f, 0f, 0.7719755f)
    };

    public static ColorPalette PyroColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.5764706f, 0f, 0.03921569f),
        RightNoteColor = new Color(1f, 0.6705883f, 0f),
        LightColor1 = new Color(1f, 0.1098039f, 0.2039216f),
        LightColor2 = new Color(0.8862745f, 0.7372549f, 0.2627451f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(1f, 0f, 0.1764706f),
        BoostLightColor2 = new Color(0.7647059f, 0.7647059f, 0.7647059f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.8490566f, 0.7037643f, 0.4285333f)
    };

    public static ColorPalette EdmColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.6320754f, 0.6320754f, 0.6320754f),
        RightNoteColor = new Color(0.1764706f, 0.6980392f, 0.8784314f),
        LightColor1 = new Color(0.08220173f, 0.7169812f, 0f),
        LightColor2 = new Color(0f, 0.3671638f, 0.7169812f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.735849f, 0f, 0.1758632f),
        BoostLightColor2 = new Color(0.4284593f, 0f, 0.754717f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.1764706f, 0.6980392f, 0.8784314f)
    };

    public static ColorPalette LizzoColors => new ColorPalette
    {
        LeftNoteColor = new Color(1f, 0.8132076f, 0.3773585f),
        RightNoteColor = new Color(0.6705883f, 0.254902f, 0.8980392f),
        LightColor1 = new Color(0.8392157f, 0.6470588f, 0.2156863f),
        LightColor2 = new Color(0.8196079f, 0.2392157f, 0.8784314f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(1f, 0.4f, 0.5529412f),
        BoostLightColor2 = new Color(0.3686275f, 0.7960784f, 1f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(1f, 0.5020987f, 0.1882353f)
    };

    public static ColorPalette WeekndColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.5843138f, 0.1294118f, 0.1294118f),
        RightNoteColor = new Color(0.2235294f, 0.2901961f, 0.3294118f),
        LightColor1 = new Color(1f, 0.2979701f, 0.1411765f),
        LightColor2 = new Color(0.1668743f, 0.3753689f, 0.7075472f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.9568628f, 0.6039216f, 0.1215686f),
        BoostLightColor2 = new Color(0.5254902f, 0.8274511f, 0.9921569f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.9176471f, 0.2980392f, 0.007843138f)
    };

    public static ColorPalette RockMixtapeColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.6f, 0.4233f, 0.042f),
        RightNoteColor = new Color(0.6006f, 0.7441199f, 0.78f),
        LightColor1 = new Color(0.75f, 0.12f, 0.162f),
        LightColor2 = new Color(0.95f, 0.5820333f, 0.1615f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.96f, 0.1344f, 0.9187202f),
        BoostLightColor2 = new Color(0.378f, 0.813f, 0.9f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(1f, 1f, 1f)
    };

    public static ColorPalette Dragons2Colors => new ColorPalette
    {
        LeftNoteColor = new Color(0.7264151f, 0.6587077f, 0.2809719f),
        RightNoteColor = new Color(0.2509804f, 0.7647059f, 0.405098f),
        LightColor1 = new Color(0.01960784f, 0.9960785f, 0.06666667f),
        LightColor2 = new Color(0f, 0.05490196f, 1f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.9764706f, 0.03137255f, 0.01960784f),
        BoostLightColor2 = new Color(1f, 0.8292086f, 0.2264151f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.5548979f, 0.2470588f, 1f)
    };

    public static ColorPalette Panic2Colors => new ColorPalette
    {
        LeftNoteColor = new Color(0.9019608f, 0.3333333f, 0.5686275f),
        RightNoteColor = new Color(0.1529412f, 0.5568628f, 0.4862745f),
        LightColor1 = new Color(0.6980392f, 0.1137255f, 0.372549f),
        LightColor2 = new Color(0.1882353f, 0.6196079f, 0.6235294f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.9019608f, 0.4470589f, 0.06666667f),
        BoostLightColor2 = new Color(0.6365692f, 0.4373443f, 0.8584906f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.9686275f, 0.3803922f, 0.2745098f)
    };

    public static ColorPalette QueenColors => new ColorPalette
    {
        LeftNoteColor = new Color(0.58f, 0.5675714f, 0.5551428f),
        RightNoteColor = new Color(0.5236231f, 0.1345675f, 0.6792453f),
        LightColor1 = new Color(0.9333334f, 0.6392157f, 0.1215686f),
        LightColor2 = new Color(0.04313726f, 0.7176471f, 0.8980393f),
        WhiteLightColor = Color.white,
        BoostLightColor1 = new Color(0.7686275f, 0.145098f, 0.07450981f),
        BoostLightColor2 = new Color(0.4f, 0.007843138f, 0.7254902f),
        BoostWhiteLightColor = Color.white,
        WallColor = new Color(0.9333334f, 0.6392157f, 0.1215686f)
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