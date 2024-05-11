using System;

[Serializable]
public class BeatmapLightshowV4
{
    public string version;

    public BeatmapElementV4[] basicEvents;
    public BeatmapElementV4[] colorBoostEvents;

    public BeatmapBasicEventDataV4[] basicEventsData;
    public BeatmapColorBoostEventDataV4[] colorBoostEventsData;

    //Waypoints and event boxes left out


    public BeatmapLightshowV4()
    {
        version = "4.0.0";
        basicEvents = new BeatmapElementV4[0];
        colorBoostEvents = new BeatmapElementV4[0];
        basicEventsData = new BeatmapBasicEventDataV4[0];
        colorBoostEventsData = new BeatmapColorBoostEventDataV4[0];
    }


    public void AddNulls()
    {
        version ??= "4.0.0";
        basicEvents ??= new BeatmapElementV4[0];
        colorBoostEvents ??= new BeatmapElementV4[0];
        basicEventsData ??= new BeatmapBasicEventDataV4[0];
        colorBoostEventsData ??= new BeatmapColorBoostEventDataV4[0];
    }
}


[Serializable]
public class BeatmapBasicEventDataV4
{
    public int t;
    public int i;
    public float f;
}


[Serializable]
public class BeatmapColorBoostEventDataV4
{
    public int b;
}