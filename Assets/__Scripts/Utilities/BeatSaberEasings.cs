using UnityEngine;

public static class BeatSaberEasings
{
    public static float BeatSaberInOutBack(float t)
    {
        if(t < 0.517f)
        {
            return 5.014f * t * t * t;
        }
        else
        {
            float a = Mathf.Pow(1.665f * (t - 0.4f) - 1f, 3f);
            float b = Mathf.Pow(1.665f * (t - 0.4f) - 1f, 2f);
            return 1f + (2.70158f * a) + (1.70158f * b);
        }
    }


    public static float BeatSaberInOutElastic(float t)
    {
        if(t < 0.3f)
        {
            return 37.037f * t * t * t;
        }
        else
        {
            float pow = Mathf.Pow(2f, -10f * (t - 0.2f));
            float sin = Mathf.Sin(t * 10f * 2.0943952f);
            return pow * sin + 1f;
        }
    }


    public static float BeatSaberInOutBounce(float t)
    {
        if(t < 0.36363637f)
        {
            return 20.796f * t * t * t;
        }
        else if(t < 0.72727275f)
        {
            t -= 0.54545456f;
            return 7.5625f * t * t + 0.75f;
        }
        else if(t < 0.90909094f)
        {
            t -= 0.8181818f;
            return 7.5625f * t * t + 0.9375f;
        }
        else
        {
            t -= 0.95454544f;
            return 7.5625f * t * t + 0.984375f;
        }
    }


    public static float Ease(float t, BeatSaberEasingType easing)
    {
        switch(easing)
        {
            default:
            case BeatSaberEasingType.None:
                return t >= 1f ? 1f : 0f;
            case BeatSaberEasingType.Linear:
                return t;

            case BeatSaberEasingType.InQuad:
                return Easings.Quad.In(t);
            case BeatSaberEasingType.OutQuad:
                return Easings.Quad.Out(t);
            case BeatSaberEasingType.InOutQuad:
                return Easings.Quad.InOut(t);

            case BeatSaberEasingType.InSine:
                return Easings.Sine.In(t);
            case BeatSaberEasingType.OutSine:
                return Easings.Sine.Out(t);
            case BeatSaberEasingType.InOutSine:
                return Easings.Sine.InOut(t);
            
            case BeatSaberEasingType.InCubic:
                return Easings.Cubic.In(t);
            case BeatSaberEasingType.OutCubic:
                return Easings.Cubic.Out(t);
            case BeatSaberEasingType.InOutCubic:
                return Easings.Cubic.InOut(t);

            case BeatSaberEasingType.InQuart:
                return Easings.Quart.In(t);
            case BeatSaberEasingType.OutQuart:
                return Easings.Quart.Out(t);
            case BeatSaberEasingType.InOutQuart:
                return Easings.Quart.InOut(t);

            case BeatSaberEasingType.InQuint:
                return Easings.Quint.In(t);
            case BeatSaberEasingType.OutQuint:
                return Easings.Quint.Out(t);
            case BeatSaberEasingType.InOutQuint:
                return Easings.Quint.InOut(t);

            case BeatSaberEasingType.InExpo:
                return Easings.Expo.In(t);
            case BeatSaberEasingType.OutExpo:
                return Easings.Expo.Out(t);
            case BeatSaberEasingType.InOutExpo:
                return Easings.Expo.InOut(t);

            case BeatSaberEasingType.InCirc:
                return Easings.Circ.In(t);
            case BeatSaberEasingType.OutCirc:
                return Easings.Circ.Out(t);
            case BeatSaberEasingType.InOutCirc:
                return Easings.Circ.InOut(t);

            case BeatSaberEasingType.InBack:
                return Easings.Back.In(t);
            case BeatSaberEasingType.OutBack:
                return Easings.Back.Out(t);
            case BeatSaberEasingType.InOutBack:
                return Easings.Back.InOut(t);

            case BeatSaberEasingType.InElastic:
                return Easings.Elastic.In(t);
            case BeatSaberEasingType.OutElastic:
                return Easings.Elastic.Out(t);
            case BeatSaberEasingType.InOutElastic:
                return Easings.Elastic.InOut(t);

            case BeatSaberEasingType.InBounce:
                return Easings.Bounce.In(t);
            case BeatSaberEasingType.OutBounce:
                return Easings.Bounce.Out(t);
            case BeatSaberEasingType.InOutBounce:
                return Easings.Bounce.InOut(t);

            case BeatSaberEasingType.BeatSaberInOutBack:
                return BeatSaberInOutBack(t);
            case BeatSaberEasingType.BeatSaberInOutElastic:
                return BeatSaberInOutElastic(t);
            case BeatSaberEasingType.BeatSaberInOutBounce:
                return BeatSaberInOutBounce(t);
        }
    }


    public static float BeatmapEase(float t, BeatSaberEasingType easing)
    {
        //This is the same as Ease(), except it accounts for illegal easings
        //when loading the beatmap in game
        switch(easing)
        {
            //All these easings don't show up in game
            case BeatSaberEasingType.InSine:
            case BeatSaberEasingType.OutSine:
            case BeatSaberEasingType.InOutSine:
            
            case BeatSaberEasingType.InCubic:
            case BeatSaberEasingType.OutCubic:
            case BeatSaberEasingType.InOutCubic:

            case BeatSaberEasingType.InQuart:
            case BeatSaberEasingType.OutQuart:
            case BeatSaberEasingType.InOutQuart:

            case BeatSaberEasingType.InQuint:
            case BeatSaberEasingType.OutQuint:
            case BeatSaberEasingType.InOutQuint:

            case BeatSaberEasingType.InExpo:
            case BeatSaberEasingType.OutExpo:
            case BeatSaberEasingType.InOutExpo:
                return Ease(t, BeatSaberEasingType.None);

            default:
                return Ease(t, easing);
        }
    }
}


public enum BeatSaberEasingType
{
    None = -1,
    Linear = 0,

    InQuad = 1,
    OutQuad = 2,
    InOutQuad = 3,

    InSine = 4,
    OutSine = 5,
    InOutSine = 6,

    InCubic = 7,
    OutCubic = 8,
    InOutCubic = 9,

    InQuart = 10,
    OutQuart = 11,
    InOutQuart = 12,

    InQuint = 13,
    OutQuint = 14,
    InOutQuint = 15,

    InExpo = 16,
    OutExpo = 17,
    InOutExpo = 18,

    InCirc = 19,
    OutCirc = 20,
    InOutCirc = 21,

    InBack = 22,
    OutBack = 23,
    InOutBack = 24,

    InElastic = 25,
    OutElastic = 26,
    InOutElastic = 27,

    InBounce = 28,
    OutBounce = 29,
    InOutBounce = 30,

    BeatSaberInOutBack = 100,
    BeatSaberInOutElastic = 101,
    BeatSaberInOutBounce = 102
}