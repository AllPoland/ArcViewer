using UnityEngine;

public class Easings
{
    public static float Linear(float x)
    {
        //Extremely useful method yes
        //Only here for EasingFromString()
        return x;
    }


    public static float Step(float x)
    {
        return Mathf.Floor(x);
    }


    public static class Sine
    {
        public static float In(float x)
        {
            return 1 - Mathf.Cos((x * Mathf.PI) / 2);
        }

        public static float Out(float x)
        {
            return Mathf.Sin((x * Mathf.PI) / 2);
        }

        public static float InOut(float x)
        {
            return -(Mathf.Cos(Mathf.PI * x) - 1) / 2;
        }
    }


    public static class Quad
    {
        public static float In(float x)
        {
            return x * x;
        }

        public static float Out(float x)
        {
            return 1 - (1 - x) * (1 - x);
        }

        public static float InOut(float x)
        {
            return x < 0.5 ? 2 * x * x : 1 - Mathf.Pow(-2 * x + 2, 2) / 2;
        }
    }


    public static class Cubic
    {
        public static float In(float x)
        {
            return x * x * x;
        }

        public static float Out(float x)
        {
            return 1 - Mathf.Pow(1 - x, 3);
        }

        public static float InOut(float x)
        {
            return x < 0.5 ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2;
        }
    }


    public static class Quart
    {
        public static float In(float x)
        {
            return x * x * x * x;
        }

        public static float Out(float x)
        {
            return 1 - Mathf.Pow(1 - x, 4);
        }

        public static float InOut(float x)
        {
            return x < 0.5 ? 8 * x * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 4) / 2;
        }
    }


    public static class Quint
    {
        public static float In(float x)
        {
            return x * x * x * x * x;
        }

        public static float Out(float x)
        {
            return 1 - Mathf.Pow(1 - x, 5);
        }

        public static float InOut(float x)
        {
            return x < 0.5f ? 16 * x * x * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 5) / 2;
        }
    }


    public static class Expo
    {
        public static float In(float x)
        {
            return x == 0 ? 0 : Mathf.Pow(2, 10 * x - 10);
        }

        public static float Out(float x)
        {
            return x == 1 ? 1 : 1 - Mathf.Pow(2, -10 * x);
        }

        public static float InOut(float x)
        {
            return x == 0
                ? 0
                : x == 1
                ? 1
                : x < 0.5
                ? Mathf.Pow(2, 20 * x - 10) / 2
                : (2 - Mathf.Pow(2, -20 * x + 10)) / 2;
        }
    }


    public static class Circ
    {
        public static float In(float x)
        {
            return 1 - Mathf.Sqrt(1 - Mathf.Pow(x, 2));
        }

        public static float Out(float x)
        {
            return Mathf.Sqrt(1 - Mathf.Pow(x - 1, 2));
        }

        public static float InOut(float x)
        {
            return x < 0.5
                ? (1 - Mathf.Sqrt(1 - Mathf.Pow(2 * x, 2))) / 2
                : (Mathf.Sqrt(1 - Mathf.Pow(-2 * x + 2, 2)) + 1) / 2;
        }
    }


    public static class Back
    {
        private const float c1 = 1.70158f;
        private const float c2 = c1 * 1.525f;
        private const float c3 = c1 + 1f;

        public static float In(float x)
        {
            return c3 * x * x * x - c1 * x * x;
        }

        public static float Out(float x)
        {
            return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
        }

        public static float InOut(float x)
        {
            return x < 0.5
                ? (Mathf.Pow(2 * x, 2) * ((c2 + 1) * 2 * x - c2)) / 2
                : (Mathf.Pow(2 * x - 2, 2) * ((c2 + 1) * (x * 2 - 2) + c2) + 2) / 2;
        }
    }


    public static class Elastic
    {
        private const float c4 = (2 * Mathf.PI) / 3;
        private const float c5 = (2 * Mathf.PI) / 4.5f;

        public static float In(float x)
        {
            return x == 0
                ? 0
                : x == 1
                ? 1
                : -Mathf.Pow(2, 10 * x - 10) * Mathf.Sin((x * 10 - 10.75f) * c4);
        }

        public static float Out(float x)
        {
            return x == 0
                ? 0
                : x == 1
                ? 1
                : Mathf.Pow(2, -10 * x) * Mathf.Sin((x * 10 - 0.75f) * c4) + 1;
        }

        public static float InOut(float x)
        {
            return x == 0
                ? 0
                : x == 1
                ? 1
                : x < 0.5
                ? -(Mathf.Pow(2, 20 * x - 10) * Mathf.Sin((20 * x - 11.125f) * c5)) / 2
                : (Mathf.Pow(2, -20 * x + 10) * Mathf.Sin((20 * x - 11.125f) * c5)) / 2 + 1;
        }
    }


    public static class Bounce
    {
        private const float n1 = 7.5625f;
        private const float d1 = 2.75f;

        public static float In(float x)
        {
            return 1 - Out(1 - x);
        }

        public static float Out(float x)
        {
            if (x < 1 / d1)
            {
                return n1 * x * x;
            }
            else if (x < 2 / d1)
            {
                return n1 * (x -= 1.5f / d1) * x + 0.75f;
            }
            else if (x < 2.5 / d1)
            {
                return n1 * (x -= 2.25f / d1) * x + 0.9375f;
            }
            else
            {
                return n1 * (x -= 2.625f / d1) * x + 0.984375f;
            }
        }

        public static float InOut(float x)
        {
            return x < 0.5
                ? (1 - Out(1 - 2 * x)) / 2
                : (1 + Out(2 * x - 1)) / 2;
        }
    }


    public enum EasingType
    {
        Linear  = 0b_0000_0000,
        Sine    = 0b_0000_0001,
        Quad    = 0b_0000_0010,
        Cubic   = 0b_0000_0011,
        Quart   = 0b_0000_0100,
        Quint   = 0b_0000_0101,
        Expo    = 0b_0000_0110,
        Circ    = 0b_0000_0111,
        Back    = 0b_0000_1001,
        Elastic = 0b_0000_1010,
        Bounce  = 0b_0000_1011,
        Step    = 0b_0000_1100,

        In      = 0b_1000_0000,
        Out     = 0b_0100_0000,
        InOut   = 0b_1100_0000,
    }


    public static float EasingFromType(EasingType easingType, float x)
    {
        switch(easingType)
        {
            case EasingType.Sine | EasingType.In:
                return Sine.In(x);
            case EasingType.Sine | EasingType.Out:
                return Sine.Out(x);
            case EasingType.Sine | EasingType.InOut:
                return Sine.InOut(x);

            case EasingType.Quad | EasingType.In:
                return Quad.In(x);
            case EasingType.Quad | EasingType.Out:
                return Quad.Out(x);
            case EasingType.Quad | EasingType.InOut:
                return Quad.InOut(x);

            case EasingType.Cubic | EasingType.In:
                return Cubic.In(x);
            case EasingType.Cubic | EasingType.Out:
                return Cubic.Out(x);
            case EasingType.Cubic | EasingType.InOut:
                return Cubic.InOut(x);

            case EasingType.Quart | EasingType.In:
                return Quart.In(x);
            case EasingType.Quart | EasingType.Out:
                return Quart.Out(x);
            case EasingType.Quart | EasingType.InOut:
                return Quart.InOut(x);

            case EasingType.Quint | EasingType.In:
                return Quint.In(x);
            case EasingType.Quint | EasingType.Out:
                return Quint.Out(x);
            case EasingType.Quint | EasingType.InOut:
                return Quint.InOut(x);

            case EasingType.Expo | EasingType.In:
                return Expo.In(x);
            case EasingType.Expo | EasingType.Out:
                return Expo.Out(x);
            case EasingType.Expo | EasingType.InOut:
                return Expo.InOut(x);

            case EasingType.Circ | EasingType.In:
                return Circ.In(x);
            case EasingType.Circ | EasingType.Out:
                return Circ.Out(x);
            case EasingType.Circ | EasingType.InOut:
                return Circ.InOut(x);

            case EasingType.Back | EasingType.In:
                return Back.In(x);
            case EasingType.Back | EasingType.Out:
                return Back.Out(x);
            case EasingType.Back | EasingType.InOut:
                return Back.InOut(x);

            case EasingType.Elastic | EasingType.In:
                return Elastic.In(x);
            case EasingType.Elastic | EasingType.Out:
                return Elastic.Out(x);
            case EasingType.Elastic | EasingType.InOut:
                return Elastic.InOut(x);

            case EasingType.Bounce | EasingType.In:
                return Bounce.In(x);
            case EasingType.Bounce | EasingType.Out:
                return Bounce.Out(x);
            case EasingType.Bounce | EasingType.InOut:
                return Bounce.InOut(x);

            case EasingType.Step:
                return Step(x);

            case EasingType.Linear:
            default:
                return Linear(x);
        }
    }


    public static EasingType EasingTypeFromString(string easingType)
    {
        switch(easingType)
        {
            case "easeInSine":
                return EasingType.Sine | EasingType.In;
            case "easeOutSine":
                return EasingType.Sine | EasingType.Out;
            case "easeInOutSine":
                return EasingType.Sine | EasingType.InOut;

            case "easeInQuad":
                return EasingType.Quad | EasingType.In;
            case "easeOutQuad":
                return EasingType.Quad | EasingType.Out;
            case "easeInOutQuad":
                return EasingType.Quad | EasingType.InOut;

            case "easeInCubic":
                return EasingType.Cubic | EasingType.In;
            case "easeOutCubic":
                return EasingType.Cubic | EasingType.Out;
            case "easeInOutCubic":
                return EasingType.Cubic | EasingType.InOut;

            case "easeInQuart":
                return EasingType.Quart | EasingType.In;
            case "easeOutQuart":
                return EasingType.Quart | EasingType.Out;
            case "easeInOutQuart":
                return EasingType.Quart | EasingType.InOut;

            case "easeInQuint":
                return EasingType.Quint | EasingType.In;
            case "easeOutQuint":
                return EasingType.Quint | EasingType.Out;
            case "easeInOutQuint":
                return EasingType.Quint | EasingType.InOut;

            case "easeInExpo":
                return EasingType.Expo | EasingType.In;
            case "easeOutExpo":
                return EasingType.Expo | EasingType.Out;
            case "easeInOutExpo":
                return EasingType.Expo | EasingType.InOut;

            case "easeInCirc":
                return EasingType.Circ | EasingType.In;
            case "easeOutCirc":
                return EasingType.Circ | EasingType.Out;
            case "easeInOutCirc":
                return EasingType.Circ | EasingType.InOut;

            case "easeInBack":
                return EasingType.Back | EasingType.In;
            case "easeOutBack":
                return EasingType.Back | EasingType.Out;
            case "easeInOutBack":
                return EasingType.Back | EasingType.InOut;

            case "easeInElastic":
                return EasingType.Elastic | EasingType.In;
            case "easeOutElastic":
                return EasingType.Elastic | EasingType.Out;
            case "easeInOutElastic":
                return EasingType.Elastic | EasingType.InOut;

            case "easeInBounce":
                return EasingType.Bounce | EasingType.In;
            case "easeOutBounce":
                return EasingType.Bounce | EasingType.Out;
            case "easeInOutBounce":
                return EasingType.Bounce | EasingType.InOut;

            case "easeStep":
                return EasingType.Step;

            case "easeLinear":
            default:
                return EasingType.Linear;
        }
    }
}