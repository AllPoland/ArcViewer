using System;

[Serializable]
public class BeatmapDifficultyV3 : BeatmapDifficulty
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

    public BeatmapCustomDifficultyData customData;
    public bool useNormalEventsAsCompatibleEvents;

    public bool HasObjects => colorNotes.Length + bombNotes.Length
        + obstacles.Length + sliders.Length
        + burstSliders.Length + basicBeatMapEvents.Length
        + colorBoostBeatMapEvents.Length + bpmEvents.Length
        + rotationEvents.Length > 0;

    public override string Version => version;
    public override BeatmapBpmEvent[] BpmEvents => bpmEvents;
    public override BeatmapRotationEvent[] RotationEvents => rotationEvents;
    public override BeatmapColorNote[] Notes => colorNotes;
    public override BeatmapBombNote[] Bombs => bombNotes;
    public override BeatmapObstacle[] Walls => obstacles;
    public override BeatmapSlider[] Arcs => sliders;
    public override BeatmapBurstSlider[] Chains => burstSliders;

    public override BeatmapBasicBeatmapEvent[] BasicEvents => basicBeatMapEvents;
    public override BeatmapColorBoostBeatmapEvent[] BoostEvents => colorBoostBeatMapEvents;

    public override BeatmapCustomDifficultyData CustomData => customData;


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


    public override void AddNulls()
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