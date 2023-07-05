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


    public delegate float EasingDelegate(float x);

    public static EasingDelegate EasingFromString(string easingName)
    {
        switch(easingName)
        {
            case "easeInSine":
                return Sine.In;
            case "easeOutSine":
                return Sine.Out;
            case "easeInOutSine":
                return Sine.InOut;

            case "easeInQuad":
                return Quad.In;
            case "easeOutQuad":
                return Quad.Out;
            case "easeInOutQuad":
                return Quad.InOut;

            case "easeInCubic":
                return Cubic.In;
            case "easeOutCubic":
                return Cubic.Out;
            case "easeInOutCubic":
                return Cubic.InOut;

            case "easeInQuart":
                return Quart.In;
            case "easeOutQuart":
                return Quart.Out;
            case "easeInOutQuart":
                return Quart.InOut;

            case "easeInQuint":
                return Quint.In;
            case "easeOutQuint":
                return Quint.Out;
            case "easeInOutQuint":
                return Quint.InOut;

            case "easeInExpo":
                return Expo.In;
            case "easeOutExpo":
                return Expo.Out;
            case "easeInOutExpo":
                return Expo.InOut;

            case "easeInCirc":
                return Circ.In;
            case "easeOutCirc":
                return Circ.Out;
            case "easeInOutCirc":
                return Circ.InOut;

            case "easeInBack":
                return Back.In;
            case "easeOutBack":
                return Back.Out;
            case "easeInOutBack":
                return Back.InOut;

            case "easeInElastic":
                return Elastic.In;
            case "easeOutElastic":
                return Elastic.Out;
            case "easeInOutElastic":
                return Elastic.InOut;

            case "easeInBounce":
                return Bounce.In;
            case "easeOutBounce":
                return Bounce.Out;
            case "easeInOutBounce":
                return Bounce.InOut;

            case "easeStep":
                return Step;

            case "easeLinear":
            default:
                return Linear;
        }
    }
}