using System.Collections;
using System.Collections.Generic;

public class BeatmapWrapperV4 : BeatmapDifficulty
{
    public BeatmapDifficultyV4 Beatmap;

    public override string Version => Beatmap.version;

    private BeatmapColorNoteArrayV4 colorNotes;
    private BeatmapBombNoteArrayV4 bombNotes;
    private BeatmapObstacleArrayV4 obstacles;
    private BeatmapArcArrayV4 arcs;
    private BeatmapChainArrayV4 chains;

    protected override BeatmapElementList<BeatmapBpmEvent> BpmEvents => null;
    public override BeatmapElementList<BeatmapRotationEvent> RotationEvents => new BeatmapElementArray<BeatmapRotationEvent>();

    public override BeatmapElementList<BeatmapColorNote> Notes => colorNotes;
    public override BeatmapElementList<BeatmapBombNote> Bombs => bombNotes;
    public override BeatmapElementList<BeatmapObstacle> Walls => obstacles;
    public override BeatmapElementList<BeatmapSlider> Arcs => arcs;
    public override BeatmapElementList<BeatmapBurstSlider> Chains => chains;

    public override BeatmapElementList<BeatmapBasicBeatmapEvent> BasicEvents => new BeatmapElementArray<BeatmapBasicBeatmapEvent>();
    public override BeatmapElementList<BeatmapColorBoostBeatmapEvent> BoostEvents => new BeatmapElementArray<BeatmapColorBoostBeatmapEvent>();

    public override BeatmapCustomDifficultyData CustomData => null;


    public BeatmapWrapperV4()
    {
        Beatmap = new BeatmapDifficultyV4();
        Init();
    }


    public BeatmapWrapperV4(BeatmapDifficultyV4 beatmap)
    {
        Beatmap = beatmap;
        Init();
    }


    private void Init()
    {
        Beatmap.AddNulls();
        
        colorNotes = new BeatmapColorNoteArrayV4(Beatmap);
        bombNotes = new BeatmapBombNoteArrayV4(Beatmap);
        obstacles = new BeatmapObstacleArrayV4(Beatmap);
        arcs = new BeatmapArcArrayV4(Beatmap);
        chains = new BeatmapChainArrayV4(Beatmap);
    }
}


public abstract class BeatmapElementArrayV4<Output, Element, Data> : BeatmapElementList<Output>
{
    //An abstracted array emulator that lazily combines object beats and metadata

    public Element[] elements;
    public Data[] data;

    public override int Length => elements.Length;
    public override Output this[int i] => GetObjectAtIndex(i);
    public override IEnumerator<Output> GetEnumerator() => new BeatmapElementEnumV4(this);


    public BeatmapElementArrayV4(Element[] objects, Data[] objectData)
    {
        elements = objects;
        data = objectData;
    }


    public abstract Output CreateOutput(Element element);


    public T GetDataAtIndex<T>(int i, T[] values)
    {
        //Use the data index this element points to. If it's out of range, use the default
        int dataIdx = i;
        return dataIdx >= 0 && dataIdx < values.Length ? values[dataIdx] : default(T);
    }


    public Data GetDataAtIndex(int i)
    {
        return GetDataAtIndex(i, data);
    }


    public Output GetObjectAtIndex(int i)
    {
        return CreateOutput(elements[i]);
    }


    public class BeatmapElementEnumV4 : IEnumerator<Output>
    {
        private BeatmapElementArrayV4<Output, Element, Data> elementList;

        private int position = -1;

        object IEnumerator.Current => elementList.GetObjectAtIndex(position);
        public Output Current => elementList.GetObjectAtIndex(position);


        public BeatmapElementEnumV4(BeatmapElementArrayV4<Output, Element, Data> elementList)
        {
            this.elementList = elementList;
            position = -1;
        }


        public bool MoveNext()
        {
            position++;
            return position < elementList.elements.Length;
        }


        public void Reset()
        {
            position = -1;
        }


        public void Dispose() { }
    }
}


public class BeatmapColorNoteArrayV4 : BeatmapElementArrayV4<BeatmapColorNote, BeatmapColorNoteV4, BeatmapColorNoteDataV4>
{
    public BeatmapColorNoteArrayV4(BeatmapDifficultyV4 Beatmap) : base(Beatmap.colorNotes, Beatmap.colorNotesData){}

    public override BeatmapColorNote CreateOutput(BeatmapColorNoteV4 element)
    {
        BeatmapColorNoteDataV4 data = GetDataAtIndex(element.i);
        return new BeatmapColorNote
        {
            b = element.b, //Beat
            x = data.x, //X pos
            y = data.y, //Y pos
            c = data.c, //Color
            d = data.d, //Direction
            a = data.a //Angle offset
        };
    }
}


public class BeatmapBombNoteArrayV4 : BeatmapElementArrayV4<BeatmapBombNote, BeatmapBombNoteV4, BeatmapBombNoteDataV4>
{
    public BeatmapBombNoteArrayV4(BeatmapDifficultyV4 Beatmap) : base(Beatmap.bombNotes, Beatmap.bombNotesData){}

    public override BeatmapBombNote CreateOutput(BeatmapBombNoteV4 element)
    {
        BeatmapBombNoteDataV4 data = GetDataAtIndex(element.i);
        return new BeatmapBombNote
        {
            b = element.b, //Beat
            x = data.x, //X pos
            y = data.y //Y pos
        };
    }
}


public class BeatmapObstacleArrayV4 : BeatmapElementArrayV4<BeatmapObstacle, BeatmapObstacleV4, BeatmapObstacleDataV4>
{
    public BeatmapObstacleArrayV4(BeatmapDifficultyV4 Beatmap) : base(Beatmap.obstacles, Beatmap.obstaclesData){}

    public override BeatmapObstacle CreateOutput(BeatmapObstacleV4 element)
    {
        BeatmapObstacleDataV4 data =GetDataAtIndex(element.i);
        return new BeatmapObstacle
        {
            b = element.b, //Beat
            x = data.x, //X pos
            y = data.y, //Y pos
            d = data.d, //Duration
            w = data.w, //Width
            h = data.h //Height
        };
    }
}


public class BeatmapArcArrayV4 : BeatmapElementArrayV4<BeatmapSlider, BeatmapArcV4, BeatmapArcDataV4>
{
    private readonly BeatmapColorNoteDataV4[] notesData;

    public BeatmapArcArrayV4(BeatmapDifficultyV4 Beatmap) : base(Beatmap.arcs, Beatmap.arcsData)
    {
        notesData = Beatmap.colorNotesData;
    }

    public override BeatmapSlider CreateOutput(BeatmapArcV4 element)
    {
        BeatmapColorNoteDataV4 headData = GetDataAtIndex(element.hi, notesData);
        BeatmapColorNoteDataV4 tailData = GetDataAtIndex(element.ti, notesData);
        BeatmapArcDataV4 data = GetDataAtIndex(element.ai);
        return new BeatmapSlider
        {
            b = element.hb, //Head beat
            x = headData.x, //Head x pos
            y = headData.y, //Head y pos
            c = headData.c, //Color
            d = headData.d, //Head direction
            mu = data.m, //Head control mult
            tb = element.tb, //Tail beat
            tx = tailData.x, //Tail x pos
            ty = tailData.y, //Tail y pos
            tc = tailData.d, //Tail direction
            tmu = data.tm, //Tail control mult
            m = data.a //Midpoint anchor mode
        };
    }
}


public class BeatmapChainArrayV4 : BeatmapElementArrayV4<BeatmapBurstSlider, BeatmapChainV4, BeatmapChainDataV4>
{
    private readonly BeatmapColorNoteDataV4[] notesData;

    public BeatmapChainArrayV4(BeatmapDifficultyV4 Beatmap) : base(Beatmap.chains, Beatmap.chainsData)
    {
        notesData = Beatmap.colorNotesData;
    }

    public override BeatmapBurstSlider CreateOutput(BeatmapChainV4 element)
    {
        BeatmapColorNoteDataV4 headData = GetDataAtIndex(element.i, notesData);
        BeatmapChainDataV4 data = GetDataAtIndex(element.ci);
        return new BeatmapBurstSlider
        {
            b = element.hb, //Head beat
            x = headData.x, //Head x pos
            y = headData.y, //Head y pos
            c = headData.c, //Color
            d = headData.d, //Head direction
            tb = element.tb, //Tail beat
            tx = data.tx, //Tail x pos
            ty = data.ty, //Tail y pos
            sc = data.c, //Link count
            s = data.s //Squish factor
        };
    }
}