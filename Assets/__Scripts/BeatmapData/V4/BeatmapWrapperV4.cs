using System.Collections;
using System.Collections.Generic;

public class BeatmapWrapperV4 : BeatmapDifficulty
{
    public BeatmapDifficultyV4 Beatmap;
    public BeatmapLightshowV4 Lightshow;

    public override string Version => Beatmap.version;

    private BeatmapElementArray<BeatmapBpmEvent> bpmEvents;
    private BeatmapSpawnRotationArrayV4 spawnRotations;

    private BeatmapColorNoteArrayV4 colorNotes;
    private BeatmapBombNoteArrayV4 bombNotes;
    private BeatmapObstacleArrayV4 obstacles;
    private BeatmapArcArrayV4 arcs;
    private BeatmapChainArrayV4 chains;

    private BeatmapNjsEventArrayV4 njsEvents;

    private BeatmapBasicEventArrayV4 basicEvents;
    private BeatmapColorBoostEventArrayV4 colorBoostEvents;

    //Pointing bpmEvents to null uses the universal bpmEvents in info
    public override BeatmapElementList<BeatmapBpmEvent> BpmEvents => bpmEvents;
    public override BeatmapElementList<BeatmapRotationEvent> RotationEvents => spawnRotations;

    public override BeatmapElementList<BeatmapColorNote> Notes => colorNotes;
    public override BeatmapElementList<BeatmapBombNote> Bombs => bombNotes;
    public override BeatmapElementList<BeatmapObstacle> Walls => obstacles;
    public override BeatmapElementList<BeatmapSlider> Arcs => arcs;
    public override BeatmapElementList<BeatmapBurstSlider> Chains => chains;

    public override BeatmapElementList<BeatmapNjsEvent> NjsEvents => njsEvents;

    public override BeatmapElementList<BeatmapBasicBeatmapEvent> BasicEvents => basicEvents;
    public override BeatmapElementList<BeatmapColorBoostBeatmapEvent> BoostEvents => colorBoostEvents;

    public override BeatmapCustomDifficultyData CustomData => null;


    public BeatmapWrapperV4()
    {
        Beatmap = new BeatmapDifficultyV4();
        Lightshow = new BeatmapLightshowV4();
        Init();
    }


    public BeatmapWrapperV4(BeatmapDifficultyV4 beatmap, BeatmapLightshowV4 lightshow, BeatmapBpmEvent[] bpmChanges)
    {
        Beatmap = beatmap;
        Lightshow = lightshow;
        bpmEvents = bpmChanges;
        Init();
    }


    private void Init()
    {
        Beatmap.AddNulls();
        Lightshow.AddNulls();
        
        bpmEvents ??= new BeatmapBpmEvent[0];
        spawnRotations = new BeatmapSpawnRotationArrayV4(Beatmap);

        colorNotes = new BeatmapColorNoteArrayV4(Beatmap);
        bombNotes = new BeatmapBombNoteArrayV4(Beatmap);
        obstacles = new BeatmapObstacleArrayV4(Beatmap);
        arcs = new BeatmapArcArrayV4(Beatmap);
        chains = new BeatmapChainArrayV4(Beatmap);

        njsEvents = new BeatmapNjsEventArrayV4(Beatmap);

        basicEvents = new BeatmapBasicEventArrayV4(Lightshow);
        colorBoostEvents = new BeatmapColorBoostEventArrayV4(Lightshow);
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
    public BeatmapColorNoteArrayV4(BeatmapDifficultyV4 beatmap) : base(beatmap.colorNotes, beatmap.colorNotesData){}

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
    public BeatmapBombNoteArrayV4(BeatmapDifficultyV4 beatmap) : base(beatmap.bombNotes, beatmap.bombNotesData){}

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
    public BeatmapObstacleArrayV4(BeatmapDifficultyV4 beatmap) : base(beatmap.obstacles, beatmap.obstaclesData){}

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

    public BeatmapArcArrayV4(BeatmapDifficultyV4 beatmap) : base(beatmap.arcs, beatmap.arcsData)
    {
        notesData = beatmap.colorNotesData;
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

    public BeatmapChainArrayV4(BeatmapDifficultyV4 beatmap) : base(beatmap.chains, beatmap.chainsData)
    {
        notesData = beatmap.colorNotesData;
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


public class BeatmapSpawnRotationArrayV4 : BeatmapElementArrayV4<BeatmapRotationEvent, BeatmapElementV4, BeatmapSpawnRotationDataV4>
{
    public BeatmapSpawnRotationArrayV4(BeatmapDifficultyV4 beatmap) : base(beatmap.spawnRotations, beatmap.spawnRotationsData){}

    public override BeatmapRotationEvent CreateOutput(BeatmapElementV4 element)
    {
        BeatmapSpawnRotationDataV4 data = GetDataAtIndex(element.i);
        return new BeatmapRotationEvent
        {
            b = element.b, //Beat
            e = data.t, //Execution time (early/late)
            r = data.r //Rotation amount
        };
    }
}


public class BeatmapNjsEventArrayV4 : BeatmapElementArrayV4<BeatmapNjsEvent, BeatmapElementV4, BeatmapNjsEventDataV4>
{
    public BeatmapNjsEventArrayV4(BeatmapDifficultyV4 beatmap) : base(beatmap.njsEvents, beatmap.njsEventData){}

    public override BeatmapNjsEvent CreateOutput(BeatmapElementV4 element)
    {
        BeatmapNjsEventDataV4 data = GetDataAtIndex(element.i);
        return new BeatmapNjsEvent
        {
            b = element.b,
            p = data.p,
            e = data.e,
            d = data.d
        };
    }
}


public class BeatmapBasicEventArrayV4 : BeatmapElementArrayV4<BeatmapBasicBeatmapEvent, BeatmapElementV4, BeatmapBasicEventDataV4>
{
    public BeatmapBasicEventArrayV4(BeatmapLightshowV4 lightshow) : base(lightshow.basicEvents, lightshow.basicEventsData){}

    public override BeatmapBasicBeatmapEvent CreateOutput(BeatmapElementV4 element)
    {
        BeatmapBasicEventDataV4 data = GetDataAtIndex(element.i);
        return new BeatmapBasicBeatmapEvent
        {
            b = element.b, //Beat
            et = data.t, //Event type
            i = data.i, //Int value
            f = data.f //Float value
        };
    }
}


public class BeatmapColorBoostEventArrayV4 : BeatmapElementArrayV4<BeatmapColorBoostBeatmapEvent, BeatmapElementV4, BeatmapColorBoostEventDataV4>
{
    public BeatmapColorBoostEventArrayV4(BeatmapLightshowV4 lightshow) : base(lightshow.colorBoostEvents, lightshow.colorBoostEventsData){}

    public override BeatmapColorBoostBeatmapEvent CreateOutput(BeatmapElementV4 element)
    {
        BeatmapColorBoostEventDataV4 data = GetDataAtIndex(element.i);
        return new BeatmapColorBoostBeatmapEvent
        {
            b = element.b, //Beat
            o = data.b > 0 //Boost enabled
        };
    }
}