using UnityEngine;

public class Easings
{
    public class Sine
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


    public class Quad
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


    public class Cubic
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


    public class Quart
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
}