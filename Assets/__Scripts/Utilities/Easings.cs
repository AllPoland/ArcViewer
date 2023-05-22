using UnityEngine;

public class Easings
{
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
}