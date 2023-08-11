using System;
using System.Collections.Generic;
using System.IO.Compression;
using UnityEngine;

public static class Extensions
{
    ///<summary>
    ///Rotates the vector a given amount of degrees.
    ///</summary>
    public static Vector2 Rotate(this Vector2 v, float degrees)
    {
       float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
       float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);
       
       float tx = v.x;
       float ty = v.y;
       v.x = (cos * tx) - (sin * ty);
       v.y = (sin * tx) + (cos * ty);
       return v;
    }
 
 
    ///<summary>
    ///Returns a vector with the absolute values of the x, y, and z components.
    ///</summary>
    public static Vector3 Abs(this Vector3 v)
    {
       v.x = Mathf.Abs(v.x);
       v.y = Mathf.Abs(v.y);
       v.z = Mathf.Abs(v.z);
       return v;
    }
 

    ///<summary>
    ///Acts as a wrapper for System.Math.Round() because the method is dumb and annoying and uses weird casting.
    ///</summary>
    public static float Round(this float f, int digits = 0)
    {
        return (float)Math.Round(f, digits);
    }


    ///<summary>
    ///Returns true if the values are very close. This is a more lenient version of Mathf.Approximately.
    ///</summary>
    public static bool Approximately(this float f, float x)
    {
        return Mathf.Abs(f - x) < 0.0001f;
    }


    ///<summary>
    ///Removes the last instance trimString from the string, if it's found
    ///</summary>
    public static string TrimEnd(this string s, string trimString, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
    {
        if(s.EndsWith(trimString, comparison))
        {
            return s.Substring(0, s.LastIndexOf(trimString, comparison));
        }
        else return s;
    }


    public static string TrimStart(this string s, string trimString, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
    {
        if(s.StartsWith(trimString, comparison))
        {
            return s.Remove(s.IndexOf(trimString, comparison), trimString.Length);
        }
        else return s;
    }


    ///<summary>
    ///Removes any objects from the list matching the predicate, but only searching forward, with the search ending on the first object that doesn't match.
    ///</summary>
    public static void RemoveAllForward<T>(this List<T> list, Predicate<T> match)
    {
        while(list.Count > 0 && match(list[0]))
        {
            list.RemoveAt(0);
        }
    }


    public delegate float BinarySearchFloatDelegate<T>(T item);


    ///<summary>
    ///Finds all elements in the list matching the time value to 1ms, starting with a binary search. (CURRENTY BROKEN)
    ///</summary>
    public static List<T> FindAllAtTime<T>(this List<T> list, float searchValue, BinarySearchFloatDelegate<T> timeMethod)
    {
        List<T> output = new List<T>();

        int onTimeIndex = list.BinarySearchTime(searchValue, timeMethod);

        if(onTimeIndex < 0)
        {
            return output;
        }

        output.Add(list[onTimeIndex]);

        //Loop backwards and forwards and get all the rest of the notes on time
        for(int i = onTimeIndex - 1; i >= 0; i--)
        {
            if(ObjectManager.CheckSameTime(timeMethod(list[i]), searchValue))
            {
                output.Add(list[i]);
            }
        }
        for(int i = onTimeIndex + 1; i < list.Count; i++)
        {
            if(ObjectManager.CheckSameTime(timeMethod(list[i]), searchValue))
            {
                output.Add(list[i]);
            }
        }

        return output;
    }


    ///<summary>
    ///Finds any element in the list matching the time value to 1ms through binary search. Returns -1 if none exists. (CURRENTY BROKEN)
    ///</summary>
    public static int BinarySearchTime<T>(this List<T> list, float searchValue, BinarySearchFloatDelegate<T> timeMethod)
    {
        return list.BinarySearchTime(searchValue, 0, list.Count - 1, timeMethod);
    }


    ///<summary>
    ///Finds any element in the list matching the time value to 1ms through binary search, within the given min and max. Returns -1 if none exists. (CURRENTY BROKEN)
    ///</summary>
    public static int BinarySearchTime<T>(this List<T> list, float searchValue, int min, int max, BinarySearchFloatDelegate<T> timeMethod)
    {
        min = Mathf.Clamp(min, 0, list.Count - 1);
        max = Mathf.Clamp(max, 0, list.Count - 1);

        while(min <= max)
        {
            int middle = Mathf.FloorToInt((min + max) / 2);
            float value = timeMethod(list[middle]);
            if(ObjectManager.CheckSameTime(value, searchValue))
            {
                return middle;
            }
            else if(value < searchValue)
            {
                min = middle + 1;
            }
            else
            {
                max = middle - 1;
            }
        }
        return -1;
    }


    ///<summary>
    ///Returns the color adjusted with the specified HSV values.
    ///</summary>
    ///<param name="color">The base color to adjust.</param>
    ///<param name="hue">Adjusts the hue of the color. Set to null to leave the original hue.</param>
    ///<param name="saturation">Adjusts the saturation of the color. Set to null to leave the original saturation.</param>
    ///<param name="value">Adjusts the value (brightness) of the color. Set to null to leave the original value.</param>
    ///<param name="hdr">If true, the color brightness will not be clamped between 0 and 1</param>
    public static Color SetHSV(this Color color, float? hue, float? saturation, float? value, bool hdr = false)
    {
        float h;
        float s;
        float v;
        Color.RGBToHSV(color, out h, out s, out v);
        return Color.HSVToRGB(hue ?? h, saturation ?? s, value ?? v, hdr);
    }


    ///<summary>
    ///Returns the color adjusted to the specified hue value.
    ///</summary>
    public static Color SetHue(this Color color, float hue, bool hdr = false)
    {
        float s;
        float v;
        Color.RGBToHSV(color, out _, out s, out v);
        return Color.HSVToRGB(hue, s, v, hdr);
    }


    ///<summary>
    ///Returns the color adjusted to the specified saturation value.
    ///</summary>
    public static Color SetSaturation(this Color color, float saturation, bool hdr = false)
    {
        float h;
        float v;
        Color.RGBToHSV(color, out h, out _, out v);
        return Color.HSVToRGB(h, saturation, v, hdr);
    }


    ///<summary>
    ///Returns the color adjusted to the specified brightness value.
    ///</summary>
    public static Color SetValue(this Color color, float value, bool hdr = false)
    {
        float h;
        float s;
        Color.RGBToHSV(color, out h, out s, out _);
        return Color.HSVToRGB(h, s, value, hdr);
    }


    ///<summary>
    ///Linearly interpolates between two colors using HSV.
    ///</summary>
    public static Color LerpHSV(this Color a, Color b, float t)
    {
        float ha, sa, va;
        float hb, sb, vb;

        Color.RGBToHSV(a, out ha, out sa, out va);
        Color.RGBToHSV(b, out hb, out sb, out vb);

        return Color.HSVToRGB(Mathf.Lerp(ha, hb, t), Mathf.Lerp(sa, sb, t), Mathf.Lerp(va, vb, t));
    }


    public static ZipArchiveEntry GetEntryCaseInsensitive(this ZipArchive archive, string name)
    {
        foreach(ZipArchiveEntry entry in archive.Entries)
        {
            //Search the root first to prioritize it over subfolders
            if(entry.FullName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
            {
                return entry;
            }
        }
        foreach(ZipArchiveEntry entry in archive.Entries)
        {
            if(entry.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
            {
                return entry;
            }
        }
        Debug.LogWarning($"Unable to find zip archive entry {name}!");
        return null;
    }
}