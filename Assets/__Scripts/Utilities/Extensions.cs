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