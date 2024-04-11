using System;

[Serializable]
public class BeatmapDifficultyV3
{
    public string version;

    public BeatmapBpmEvent[] bpmEvents;
    public BeatmapRotationEvent[] rotationEvents;

    public BeatmapColorNote[] colorNotes;
    public BeatmapBombNote[] bombNotes;
    public BeatmapObstacle[] obstacles;
    public BeatmapSlider[] sliders;
    public BeatmapBurstSlider[] burstSliders;

    public BeatmapBasicBeatmapEvent[] basicBeatMapEvents;
    public BeatmapColorBoostBeatmapEvent[] colorBoostBeatMapEvents;

    //Waypoints and event boxes left out

    public BeatmapCustomDifficultyData customData;
    public bool useNormalEventsAsCompatibleEvents;

    public bool HasObjects => colorNotes.Length + bombNotes.Length
        + obstacles.Length + sliders.Length
        + burstSliders.Length + basicBeatMapEvents.Length
        + colorBoostBeatMapEvents.Length + bpmEvents.Length
        + rotationEvents.Length > 0;


    public BeatmapDifficultyV3()
    {
        version = "";
        bpmEvents = new BeatmapBpmEvent[0];
        rotationEvents = new BeatmapRotationEvent[0];
        colorNotes = new BeatmapColorNote[0];
        bombNotes = new BeatmapBombNote[0];
        obstacles = new BeatmapObstacle[0];
        sliders = new BeatmapSlider[0];
        burstSliders = new BeatmapBurstSlider[0];
        basicBeatMapEvents = new BeatmapBasicBeatmapEvent[0];
        colorBoostBeatMapEvents = new BeatmapColorBoostBeatmapEvent[0];
        customData = null;
        useNormalEventsAsCompatibleEvents = false;
    }


    public void AddNulls()
    {
        version ??= "3.3.0";
        bpmEvents ??= new BeatmapBpmEvent[0];
        rotationEvents ??= new BeatmapRotationEvent[0];
        colorNotes ??= new BeatmapColorNote[0];
        bombNotes ??= new BeatmapBombNote[0];
        obstacles ??= new BeatmapObstacle[0];
        sliders ??= new BeatmapSlider[0];
        burstSliders ??= new BeatmapBurstSlider[0];
        basicBeatMapEvents ??= new BeatmapBasicBeatmapEvent[0];
        colorBoostBeatMapEvents ??= new BeatmapColorBoostBeatmapEvent[0];
    }
}