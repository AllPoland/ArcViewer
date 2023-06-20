using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class BeatmapDifficulty
{
    public string version;
    public BeatmapBpmEvent[] bpmEvents;
    public BeatmapRotationEvent[] rotationEvents;
    public BeatmapColorNote[] colorNotes;
    public BeatmapBombNote[] bombNotes;
    public BeatmapObstacle[] obstacles;
    public BeatmapSlider[] sliders;
    public BeatmapBurstSlider[] burstSliders;
    //Waypoints ommitted
    public BeatmapBasicBeatmapEvent[] basicBeatMapEvents;
    public BeatmapColorBoostBeatmapEvent[] colorBoostBeatMapEvents;
    public bool useNormalEventsAsCompatibleEvents;


    public static BeatmapDifficulty Empty = new BeatmapDifficulty
    {
        version = "",
        bpmEvents = new BeatmapBpmEvent[0],
        rotationEvents = new BeatmapRotationEvent[0],
        colorNotes = new BeatmapColorNote[0],
        bombNotes = new BeatmapBombNote[0],
        obstacles = new BeatmapObstacle[0],
        sliders = new BeatmapSlider[0],
        burstSliders = new BeatmapBurstSlider[0],
        basicBeatMapEvents = new BeatmapBasicBeatmapEvent[0],
        colorBoostBeatMapEvents = new BeatmapColorBoostBeatmapEvent[0],
        useNormalEventsAsCompatibleEvents = false
    };


    public void AddNulls()
    {
        version = version ?? "3.2.0";
        bpmEvents = bpmEvents ?? new BeatmapBpmEvent[0];
        rotationEvents = rotationEvents ?? new BeatmapRotationEvent[0];
        colorNotes = colorNotes ?? new BeatmapColorNote [0];
        bombNotes = bombNotes ?? new BeatmapBombNote[0];
        obstacles = obstacles ?? new BeatmapObstacle[0];
        sliders = sliders ?? new BeatmapSlider[0];
        burstSliders = burstSliders ?? new BeatmapBurstSlider[0];
        basicBeatMapEvents = basicBeatMapEvents ?? new BeatmapBasicBeatmapEvent[0];
        colorBoostBeatMapEvents = colorBoostBeatMapEvents ?? new BeatmapColorBoostBeatmapEvent[0];
    }
}


[Serializable]
public class BeatmapBpmEvent
{
    public float b;
    public float m;
}


[Serializable]
public class BeatmapRotationEvent
{
    public float b;
    public int e;
    public float r;
}

public class BeatmapObject
{
    public float b;
    public int x;
    public int y;
}


[Serializable]
public class BeatmapColorNote : BeatmapObject
{
    public int c;
    public int d;
    public int a;

    public BeatmapCustomNoteData customData;
}


[Serializable]
public class BeatmapBombNote : BeatmapObject
{
    public BeatmapCustomObjectData customData;
}


[Serializable]
public class BeatmapObstacle : BeatmapObject
{
    public float d;
    public int w;
    public int h;

    public BeatmapCustomObstacleData customData;
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

    public BeatmapCustomObjectData customData;
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
    public List<int> lightID;
    public float[] color;
    public string easing;
    public string lerpType;

    //Laser speed specific data
    public bool? lockRotation;

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
        List<int> val = null;
        if (reader.TokenType == JsonToken.StartObject)
        {
            int id = serializer.Deserialize<int>(reader);
            val = new List<int>() { id };
        }
        else if (reader.TokenType == JsonToken.StartArray)
        {
            val = serializer.Deserialize<List<int>>(reader);
        }
        return val;
    }

    public override bool CanConvert(Type objectType)
    {
        return true;
    }
}