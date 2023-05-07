using System;
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
    ///Returns a vector with the opposite values of the x, y, and z components.
    ///</summary>
    public static Vector3 Invert(this Vector3 v)
    {
       v.x = -v.x;
       v.y = -v.y;
       v.z = -v.z;
       return v;
    }
 

    ///<summary>
    ///Returns true if the values are very close. This is a more lenient version of Mathf.Approximately.
    ///</summary>
    public static bool Approximately(this float f, float x)
    {
        float diff = Mathf.Abs(f - x);
        return diff < 0.00001f;
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


    public static ZipArchiveEntry GetEntryCaseInsensitive(this ZipArchive archive, string name)
    {
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