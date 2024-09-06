using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public abstract class BeatmapDifficulty
{
    public abstract string Version { get; }

    public abstract BeatmapElementList<BeatmapBpmEvent> BpmEvents { get; }
    public abstract BeatmapElementList<BeatmapRotationEvent> RotationEvents { get; }

    public abstract BeatmapElementList<BeatmapColorNote> Notes { get; }
    public abstract BeatmapElementList<BeatmapBombNote> Bombs { get; }
    public abstract BeatmapElementList<BeatmapObstacle> Walls { get; }
    public abstract BeatmapElementList<BeatmapSlider> Arcs { get; }
    public abstract BeatmapElementList<BeatmapBurstSlider> Chains { get; }

    public abstract BeatmapElementList<BeatmapBasicBeatmapEvent> BasicEvents { get; }
    public abstract BeatmapElementList<BeatmapColorBoostBeatmapEvent> BoostEvents { get; }

    public abstract BeatmapCustomDifficultyData CustomData { get; }


    public static BeatmapDifficulty GetDefault()
    {
        //This is abstracted from the constructor to easily change the default later
        return new BeatmapWrapperV3();
    }
}


public abstract class BeatmapElementList<T> : IEnumerable<T>
{
    public abstract int Length { get; }
    public abstract T this[int i] { get; }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public abstract IEnumerator<T> GetEnumerator();
}


public class BeatmapBpmEvent
{
    public float b;
    public float m;
}


public class BeatmapRotationEvent
{
    public float b;
    public float e;
    public float r;
}


public abstract class BeatmapObject
{
    public float b;
    public int x;
    public int y;

    public abstract void Mirror();


    public int MirrorPosition(int x)
    {
        if(Mathf.Abs(x) >= 1000)
        {
            float position = ObjectManager.MappingExtensionsPrecision(x);
            position = -(position - 2) + 1;
            return Mathf.RoundToInt((position * 1000) + Mathf.Sign(position) * 1000);
        }
        else return -(x - 2) + 1;
    }


    public float MirrorNoodlePosition(float x)
    {
        //Noodle coordinates are centered around the middle right column
        return -(x + 0.5f) - 0.5f;
    }


    public int MirrorDirection(int d)
    {
        if(d >= 1000)
        {
            int angle = (d - 1000) % 360;
            if(angle > 180)
            {
                angle -= 360;
            }
            angle *= -1;
            
            return 1000 + (angle < 0 ? angle + 360 : angle);
        }
        else return ObjectManager.MirroredCutDirection[Mathf.Clamp(d, 0, 8)];
    }
}


[Serializable]
public class BeatmapCustomDifficultyData
{
    public BeatmapCustomBookmark[] bookmarks;
}


[Serializable]
public class BeatmapCustomBookmark
{
    public float b;
    public string n;
    public float[] c; 
}


[Serializable]
public class BeatmapColorNote : BeatmapObject
{
    public int c;
    public int d;
    public int a;

    public BeatmapCustomNoteData customData;

    
    public override void Mirror()
    {
        x = MirrorPosition(x);
        c = c == 1 ? 0 : 1;
        d = MirrorDirection(d);
        a = -a;

        if(customData != null)
        {
            if(customData.coordinates != null && customData.coordinates.Length != 0)
            {
                customData.coordinates[0] = MirrorNoodlePosition(customData.coordinates[0]);
            }
            if(customData.angle != null)
            {
                customData.angle = -customData.angle;
            }
        }
    }
}


[Serializable]
public class BeatmapBombNote : BeatmapObject
{
    public BeatmapCustomObjectData customData;


    public override void Mirror()
    {
        x = MirrorPosition(x);

        if(customData?.coordinates != null && customData.coordinates.Length != 0)
        {
            customData.coordinates[0] = MirrorNoodlePosition(customData.coordinates[0]);
        }
    }
}


[Serializable]
public class BeatmapObstacle : BeatmapObject
{
    public float d;
    public int w;
    public int h;

    public BeatmapCustomObstacleData customData;


    public override void Mirror()
    {
        if(Mathf.Abs(x) >= 1000 || Mathf.Abs(w) >= 1000)
        {
            float width = ObjectManager.MappingExtensionsPrecision(w);
            float position = ObjectManager.MappingExtensionsPrecision(x);
            position = -(position + (width - 1) - 2) + 1;
            x = Mathf.RoundToInt((position * 1000) + Mathf.Sign(position) * 1000);
        }
        else x = MirrorPosition(x + (w - 1));

        if(customData != null)
        {
            if(customData.coordinates != null && customData.coordinates.Length != 0)
            {
                customData.coordinates[0] = -customData.coordinates[0];
            }
            if(customData.size != null && customData.size.Length != 0)
            {
                customData.size[0] = -customData.size[0];
            }
        }
    }
}


[Serializable]
public class BeatmapSlider : BeatmapObject
{
    public int c;
    public int d;
    public float mu;
    public float tb;
    public int tx;
    public int ty;
    public int tc;
    public float tmu;
    public int m;

    public BeatmapCustomSliderData customData;


    public override void Mirror()
    {
        x = MirrorPosition(x);
        c = c == 1 ? 0 : 1;
        d = MirrorDirection(d);

        tx = -(tx - 2) + 1;
        tc = MirrorDirection(tc);

        m = m == 0 ? 0
            : m == 1 ? 2 : 1;

        if(customData != null)
        {
            if(customData.coordinates != null && customData.coordinates.Length != 0)
            {
                customData.coordinates[0] = MirrorNoodlePosition(customData.coordinates[0]);
            }
            if(customData.tailCoordinates != null && customData.tailCoordinates.Length != 0)
            {
                customData.tailCoordinates[0] = MirrorNoodlePosition(customData.tailCoordinates[0]);
            }
        }
    }
}


[Serializable]
public class BeatmapBurstSlider : BeatmapObject
{
    public int c;
    public int d;
    public float tb;
    public int tx;
    public int ty;
    public int sc;
    public float s;

    public BeatmapCustomSliderData customData;


    public override void Mirror()
    {
        x = MirrorPosition(x);
        c = c == 1 ? 0 : 1;
        d = MirrorDirection(d);

        tx = -(tx - 2) + 1;

        if(customData != null)
        {
            if(customData.coordinates != null && customData.coordinates.Length != 0)
            {
                customData.coordinates[0] = MirrorNoodlePosition(customData.coordinates[0]);
            }
            if(customData.tailCoordinates != null && customData.tailCoordinates.Length != 0)
            {
                customData.tailCoordinates[0] = MirrorNoodlePosition(customData.tailCoordinates[0]);
            }
        }
    }
}


[Serializable]
public class BeatmapBasicBeatmapEvent
{
    public float b;
    public int et;
    public int i;
    public float f;

    public BeatmapCustomBasicEventData customData;
}


[Serializable]
public class BeatmapColorBoostBeatmapEvent
{
    public float b;
    public bool o;
}


[Serializable]
public class BeatmapCustomObjectData
{
    public float[] coordinates;
    public float[] color;
}


[Serializable]
public class BeatmapCustomNoteData : BeatmapCustomObjectData
{
    //Angle isn't a thing in V3 noodle. This just makes it easier to carry custom _cutDirection into V3
    public float? angle;
}


[Serializable]
public class BeatmapCustomObstacleData : BeatmapCustomObjectData
{
    public float[] size;
}


[Serializable]
public class BeatmapCustomSliderData : BeatmapCustomObjectData
{
    public float[] tailCoordinates;
}


[Serializable]
public class BeatmapCustomBasicEventData
{
    [JsonConverter(typeof(LightIDConverter))]
    public int[] lightID;
    public float[] color;
    public string easing;
    public string lerpType;

    //Laser speed specific data
    [JsonConverter(typeof(StringBooleanConverter))]
    public bool lockRotation;

    //Ring specific data
    public string nameFilter;
    public float? rotation;
    public float? step;
    public float? prop;
    
    //Shared by both rings and lasers
    public float? speed;
    public int? direction;
}


//A custom json deserializer that converts a single non-array lightID into a list with one element
public class LightIDConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }


    public override object ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        int[] val = null;
        if(reader.TokenType == JsonToken.StartObject)
        {
            int id = serializer.Deserialize<int>(reader);
            val = new int[] { id };
        }
        else if(reader.TokenType == JsonToken.StartArray)
        {
            val = serializer.Deserialize<int[]>(reader);
        }
        return val;
    }


    public override bool CanConvert(Type objectType)
    {
        return true;
    }
}


public class StringBooleanConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }


    public override object ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        bool val = false;
        if(reader.TokenType == JsonToken.Boolean)
        {
            val = serializer.Deserialize<bool>(reader);
        }
        else if(reader.TokenType == JsonToken.String)
        {
            string stringValue = serializer.Deserialize<string>(reader);
            val = bool.TryParse(stringValue, out bool parsedVal) && parsedVal;
        }
        return val;
    }


    public override bool CanConvert(Type objectType)
    {
        return true;
    }
}