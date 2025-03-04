using System.Collections.Generic;

public class BeatmapWrapperV3 : BeatmapDifficulty
{
    public BeatmapDifficultyV3 Beatmap;

    public override string Version => Beatmap.version;

    public override BeatmapElementList<BeatmapBpmEvent> BpmEvents => bpmEvents;
    public override BeatmapElementList<BeatmapRotationEvent> RotationEvents => rotationEvents;

    public override BeatmapElementList<BeatmapColorNote> Notes => colorNotes;
    public override BeatmapElementList<BeatmapBombNote> Bombs => bombNotes;
    public override BeatmapElementList<BeatmapObstacle> Walls => obstacles;
    public override BeatmapElementList<BeatmapSlider> Arcs => sliders;
    public override BeatmapElementList<BeatmapBurstSlider> Chains => burstSliders;

    public override BeatmapElementList<BeatmapNjsEvent> NjsEvents => njsEvents;

    public override BeatmapElementList<BeatmapBasicBeatmapEvent> BasicEvents => basicBeatMapEvents;
    public override BeatmapElementList<BeatmapColorBoostBeatmapEvent> BoostEvents => colorBoostBeatMapEvents;

    public override BeatmapCustomDifficultyData CustomData => Beatmap.customData;

    private BeatmapElementArray<BeatmapBpmEvent> bpmEvents;
    private BeatmapElementArray<BeatmapRotationEvent> rotationEvents;
    private BeatmapElementArray<BeatmapColorNote> colorNotes;
    private BeatmapElementArray<BeatmapBombNote> bombNotes;
    private BeatmapElementArray<BeatmapObstacle> obstacles;
    private BeatmapElementArray<BeatmapSlider> sliders;
    private BeatmapElementArray<BeatmapBurstSlider> burstSliders;

    private BeatmapElementArray<BeatmapNjsEvent> njsEvents;

    private BeatmapElementArray<BeatmapBasicBeatmapEvent> basicBeatMapEvents;
    private BeatmapElementArray<BeatmapColorBoostBeatmapEvent> colorBoostBeatMapEvents;


    public BeatmapWrapperV3()
    {
        Beatmap = new BeatmapDifficultyV3();
        Init();
    }


    public BeatmapWrapperV3(BeatmapDifficultyV3 beatmap)
    {
        Beatmap = beatmap;
        Init();
    }


    private void Init()
    {
        Beatmap.AddNulls();

        bpmEvents = Beatmap.bpmEvents;
        rotationEvents = Beatmap.rotationEvents;
        colorNotes = Beatmap.colorNotes;
        bombNotes = Beatmap.bombNotes;
        obstacles = Beatmap.obstacles;
        sliders = Beatmap.sliders;
        burstSliders = Beatmap.burstSliders;

        njsEvents ??= new BeatmapElementArray<BeatmapNjsEvent>();

        basicBeatMapEvents = Beatmap.basicBeatMapEvents;
        colorBoostBeatMapEvents = Beatmap.colorBoostBeatMapEvents;
    }
}


public class BeatmapElementArray<T> : BeatmapElementList<T>
{
    //An array container to fit the abstract BeatmapDifficulty implementation
    private readonly T[] innerArray;

    public override int Length => innerArray.Length;
    public override T this[int i] => innerArray[i];
    public override IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)innerArray).GetEnumerator();

    public BeatmapElementArray()
    {
        innerArray = new T[0];
    }

    public BeatmapElementArray(T[] elements)
    {
        innerArray = elements;
    }

    public static implicit operator BeatmapElementArray<T>(T[] elements) => new BeatmapElementArray<T>(elements);
}