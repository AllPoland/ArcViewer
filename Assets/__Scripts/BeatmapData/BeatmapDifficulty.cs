using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    public BeatmapCustomObjectData customData;
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
}


[Serializable]
public class BeatmapBasicBeatmapEvent
{
    public float b;
    public int et;
    public int i;
    public float f;
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