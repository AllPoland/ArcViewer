using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatmapV4Wrapper : BeatmapDifficulty
{
    public BeatmapDifficultyV4 Beatmap;

    public override string Version => Beatmap.version;

    private BeatmapColorNoteArrayV4 colorNotes;
    private BeatmapBombNoteArrayV4 bombNotes;
    private BeatmapObstacleArrayV4 obstacles;
    private BeatmapArcArrayV4 arcs;
    private BeatmapChainArrayV4 chains;

    public override BeatmapElementList<BeatmapBpmEvent> BpmEvents => new BeatmapElementArray<BeatmapBpmEvent>();
    public override BeatmapElementList<BeatmapRotationEvent> RotationEvents => new BeatmapElementArray<BeatmapRotationEvent>();
    public override BeatmapElementList<BeatmapColorNote> Notes => colorNotes;
    public override BeatmapElementList<BeatmapBombNote> Bombs => bombNotes;
    public override BeatmapElementList<BeatmapObstacle> Walls => obstacles;
    public override BeatmapElementList<BeatmapSlider> Arcs => arcs;
    public override BeatmapElementList<BeatmapBurstSlider> Chains => chains;

    public override BeatmapElementList<BeatmapBasicBeatmapEvent> BasicEvents => new BeatmapElementArray<BeatmapBasicBeatmapEvent>();
    public override BeatmapElementList<BeatmapColorBoostBeatmapEvent> BoostEvents => new BeatmapElementArray<BeatmapColorBoostBeatmapEvent>();

    public override BeatmapCustomDifficultyData CustomData => null;


    public BeatmapV4Wrapper()
    {
        Beatmap = new BeatmapDifficultyV4();
        Init();
    }


    public BeatmapV4Wrapper(BeatmapDifficultyV4 beatmap)
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


public abstract class BeatmapElementArrayV4<Output, Element, Data> : BeatmapElementList<Output> where Element : BeatmapElementV4
{
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


    public Output GetObjectAtIndex(int i)
    {
        Element currentElement = elements[i];

        //Use the data index this element points to. If it's out of range, use the default
        int dataIdx = currentElement.i;
        Data currentData = dataIdx >= 0 && dataIdx < data.Length ? data[dataIdx] : default(Data);

        return CreateObject(currentElement, currentData);
    }


    public abstract Output CreateObject(Element element, Data data);


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

    public override BeatmapColorNote CreateObject(BeatmapColorNoteV4 element, BeatmapColorNoteDataV4 data)
    {
        return new BeatmapColorNote
        {
            b = element.b,
            x = data.x,
            y = data.y,
            c = data.c,
            d = data.d,
            a = data.a
        };
    }
}


public class BeatmapBombNoteArrayV4 : BeatmapElementArrayV4<BeatmapBombNote, BeatmapBombNoteV4, BeatmapBombNoteDataV4>
{
    public BeatmapBombNoteArrayV4(BeatmapDifficultyV4 Beatmap) : base(Beatmap.bombNotes, Beatmap.bombNotesData){}

    public override BeatmapBombNote CreateObject(BeatmapBombNoteV4 element, BeatmapBombNoteDataV4 data)
    {
        return new BeatmapBombNote
        {
            b = element.b,
            x = data.x,
            y = data.y
        };
    }
}


public class BeatmapObstacleArrayV4 : BeatmapElementArrayV4<BeatmapObstacle, BeatmapObstacleV4, BeatmapObstacleDataV4>
{
    public BeatmapObstacleArrayV4(BeatmapDifficultyV4 Beatmap) : base(Beatmap.obstacles, Beatmap.obstaclesData){}

    public override BeatmapObstacle CreateObject(BeatmapObstacleV4 element, BeatmapObstacleDataV4 data)
    {
        Debug.Log(element.i);
        return new BeatmapObstacle
        {
            b = element.b,
            x = data.x,
            y = data.y,
            d = data.d,
            w = data.w,
            h = data.h
        };
    }
}


public class BeatmapArcArrayV4 : BeatmapElementArrayV4<BeatmapSlider, BeatmapArcV4, BeatmapArcDataV4>
{
    public BeatmapArcArrayV4(BeatmapDifficultyV4 Beatmap) : base(Beatmap.arcs, Beatmap.arcsData){}

    public override BeatmapSlider CreateObject(BeatmapArcV4 element, BeatmapArcDataV4 data)
    {
        return new BeatmapSlider
        {
            b = element.b,
        };
    }
}


public class BeatmapChainArrayV4 : BeatmapElementArrayV4<BeatmapBurstSlider, BeatmapChainV4, BeatmapChainDataV4>
{
    public BeatmapChainArrayV4(BeatmapDifficultyV4 Beatmap) : base(Beatmap.chains, Beatmap.chainsData){}

    public override BeatmapBurstSlider CreateObject(BeatmapChainV4 element, BeatmapChainDataV4 data)
    {
        return new BeatmapBurstSlider
        {
            b = element.b,
        };
    }
}