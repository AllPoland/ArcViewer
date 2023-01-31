using System;

[Serializable]
public struct BeatmapDifficulty
{
    public string version;
    public BpmEvent[] bpmEvents;
    public RotationEvent[] rotationEvents;
    public ColorNote[] colorNotes;
    public BombNote[] bombNotes;
    public Obstacle[] obstacles;
    public ArcSlider[] sliders;
    public BurstSlider[] burstSliders;
    //Waypoints ommitted
    public BasicBeatmapEvent[] basicBeatMapEvents;
    public ColorBoostBeatmapEvent[] colorBoostBeatMapEvents;
    public bool useNormalEventsAsCompatibleEvents;


    public static BeatmapDifficulty Empty = new BeatmapDifficulty
    {
        version = "",
        bpmEvents = new BpmEvent[0],
        rotationEvents = new RotationEvent[0],
        colorNotes = new ColorNote[0],
        bombNotes = new BombNote[0],
        obstacles = new Obstacle[0],
        sliders = new ArcSlider[0],
        burstSliders = new BurstSlider[0],
        basicBeatMapEvents = new BasicBeatmapEvent[0],
        colorBoostBeatMapEvents = new ColorBoostBeatmapEvent[0],
        useNormalEventsAsCompatibleEvents = false
    };
}


[Serializable]
public struct BpmEvent
{
    public float b;
    public float m;
}


[Serializable]
public struct RotationEvent
{
    public float b;
    public int e;
    public float r;
}


[Serializable]
public struct ColorNote
{
    public float b;
    public int x;
    public int y;
    public int c;
    public int d;
    public int a;

    public CustomObjectData customData;
}


[Serializable]
public struct BombNote
{
    public float b;
    public int x;
    public int y;

    public CustomObjectData customData;
}


[Serializable]
public struct Obstacle
{
    public float b;
    public int x;
    public int y;
    public float d;
    public int w;
    public int h;

    public CustomObstacleData customData;
}


[Serializable]
public struct ArcSlider
{
    public float b;
    public int c;
    public int x;
    public int y;
    public int d;
    public float mu;
    public float tb;
    public int tx;
    public int ty;
    public int tc;
    public float tmu;
    public int m;

    public CustomSliderData customData;
}


[Serializable]
public struct BurstSlider
{
    public float b;
    public int x;
    public int y;
    public int c;
    public int d;
    public float tb;
    public int tx;
    public int ty;
    public int sc;
    public float s;
}


[Serializable]
public struct BasicBeatmapEvent
{
    public float b;
    public int et;
    public int i;
    public float f;
}


[Serializable]
public struct ColorBoostBeatmapEvent
{
    public float b;
    public bool o;
}


[Serializable]
public class CustomObjectData
{
    public float[] coordinates;
}


[Serializable]
public class CustomObstacleData : CustomObjectData
{
    public float[] size;
}


[Serializable]
public class CustomSliderData : CustomObjectData
{
    public float[] tailCoordinates;
}