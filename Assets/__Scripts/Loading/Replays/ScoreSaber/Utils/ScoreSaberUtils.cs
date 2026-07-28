using System;
using System.Text;
using UnityEngine;

public static class ScoreSaberUtils
{
    private static readonly byte[] MagicBytes = {
        0x53, 0x63, 0x6F, 0x72, 0x65, 0x53, 0x61, 0x62, 0x65, 0x72, 0x20,
        0x52, 0x65, 0x70, 0x6C, 0x61, 0x79, 0x20,
        0xF0, 0x9F, 0x91, 0x8C,
        0xF0, 0x9F, 0xA4, 0xA0,
        0x0D, 0x0A
    };
    public const int MagicLength = 28;

    public static bool HasMagicHeader(byte[] data, int length)
    {
        if(length < MagicLength) return false;
        for(int i = 0; i < MagicLength; i++)
            if(data[i] != MagicBytes[i]) return false;
        return true;
    }

    public static bool HasLegacyHeader(byte[] data, int length)
    {
        return length >= 4 && data[0] == 0x5D && data[1] == 0x00 && data[2] == 0x00 && data[3] == 0x80;
    }

    public static bool VersionAtLeast(string version, int major, int minor, int patch)
    {
        if(string.IsNullOrEmpty(version)) return false;
        string[] parts = version.Split('.');
        int v0 = parts.Length > 0 && int.TryParse(parts[0], out int p0) ? p0 : 0;
        int v1 = parts.Length > 1 && int.TryParse(parts[1], out int p1) ? p1 : 0;
        int v2 = parts.Length > 2 && int.TryParse(parts[2], out int p2) ? p2 : 0;
        if(v0 != major) return v0 > major;
        if(v1 != minor) return v1 > minor;
        return v2 >= patch;
    }

    public static int ReadInt(byte[] data, ref int offset)
    {
        int value = BitConverter.ToInt32(data, offset);
        offset += 4;
        return value;
    }

    public static float ReadFloat(byte[] data, ref int offset)
    {
        float value = BitConverter.ToSingle(data, offset);
        offset += 4;
        return value;
    }

    public static bool ReadBool(byte[] data, ref int offset)
    {
        bool value = BitConverter.ToBoolean(data, offset);
        offset += 1;
        return value;
    }

    public static string ReadString(byte[] data, ref int offset)
    {
        int length = BitConverter.ToInt32(data, offset);
        string value = Encoding.UTF8.GetString(data, offset + 4, length);
        offset += length + 4;
        return value;
    }

    public static string[] ReadStringArray(byte[] data, ref int offset)
    {
        int count = ReadInt(data, ref offset);
        string[] values = new string[count];
        for(int i = 0; i < count; i++)
            values[i] = ReadString(data, ref offset);
        return values;
    }

    public static Vector3 ReadVector3(byte[] data, ref int offset)
    {
        return new Vector3(
            ReadFloat(data, ref offset),
            ReadFloat(data, ref offset),
            ReadFloat(data, ref offset));
    }

    public static PositionData ReadPositionData(byte[] data, ref int offset)
    {
        return new PositionData
        {
            position = ReadVector3(data, ref offset),
            rotation = new Quaternion(
                ReadFloat(data, ref offset), ReadFloat(data, ref offset),
                ReadFloat(data, ref offset), ReadFloat(data, ref offset))
        };
    }
}
