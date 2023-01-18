using System;
using System.Collections;
using System.Collections.Generic;

[Serializable] public class BeatmapDifficultyV2
{
    public string _version;
    public NoteV2[] _notes;
    public SliderV2[] _sliders;
    public ObstacleV2[] _obstacles;
    public EventV2[] _events;
    //Waypoints ommitted


    public BeatmapDifficulty ConvertToV3()
    {
        BeatmapDifficulty converted = new BeatmapDifficulty();

        converted.version = "3.0.0";

        List<ColorNote> colorNotes = new List<ColorNote>();
        List<BombNote> bombNotes = new List<BombNote>();
        foreach(NoteV2 n in _notes)
        {
            //Loop over each note and add it to our V3 difficulty
            if(n._type == 2)
            {
                //Unused note type, this shouldn't be here
                continue;
            }
            else if(n._type < 2)
            {
                //Color note
                colorNotes.Add(
                    new ColorNote
                    {
                        b = n._time,
                        x = n._lineIndex,
                        y = n._lineLayer,
                        c = n._type,
                        d = n._cutDirection,
                        a = 0
                    }
                );
            }
            else if(n._type == 3)
            {
                //Bomb note
                bombNotes.Add
                (
                    new BombNote
                    {
                        b = n._time,
                        x = n._lineIndex,
                        y = n._lineLayer
                    }
                );
            }
        }
        converted.colorNotes = colorNotes.ToArray();
        converted.bombNotes = bombNotes.ToArray();


        List<Obstacle> obstacles = new List<Obstacle>();
        foreach(ObstacleV2 o in _obstacles)
        {
            obstacles.Add
            (
                new Obstacle
                {
                    b = o._time,
                    x = o._lineIndex,
                    y = o._type == 0 ? 0 : 2,
                    d = o._duration,
                    w = o._width,
                    h = o._type == 0 ? 5 : 3
                }
            );
        }
        converted.obstacles = obstacles.ToArray();


        List<ArcSlider> sliders = new List<ArcSlider>();
        foreach(SliderV2 s in _sliders)
        {
            sliders.Add
            (
                new ArcSlider
                {
                    b = s._headTime,
                    c = s._colorType,
                    x = s._headLineIndex,
                    y = s._headLineLayer,
                    d = s._headCutDirection,
                    mu = s._headControlPointLengthMultiplier,
                    tb = s._tailTime,
                    tx = s._tailLineIndex,
                    ty = s._tailLineLayer,
                    tc = s._tailCutDirection,
                    tmu = s._tailControlPointLengthMultiplier,
                    m = s._sliderMidAnchorMode
                }
            );
        }
        converted.sliders = sliders.ToArray();


        List<RotationEvent> rotationEvents = new List<RotationEvent>();
        List<BasicBeatmapEvent> basicBeatmapEvents = new List<BasicBeatmapEvent>();
        List<ColorBoostBeatmapEvent> colorBoostBeatmapEvents = new List<ColorBoostBeatmapEvent>();
        foreach(EventV2 e in _events)
        {
            if(e._type == 14 || e._type == 15)
            {
                //Rotation event
                rotationEvents.Add
                (
                    new RotationEvent
                    {
                        b = e._time,
                        e = e._type - 14,  //subtracting 14 from the type makes it line up with the expected 0 and 1 in V3 format
                        r = e._floatValue
                    }
                );
            }
            else if(e._type == 5)
            {
                //Bost color event
                colorBoostBeatmapEvents.Add
                (
                    new ColorBoostBeatmapEvent
                    {
                        b = e._time,
                        o = e._value > 0
                    }
                );
            }
            else
            {
                //Other event
                basicBeatmapEvents.Add
                (
                    new BasicBeatmapEvent
                    {
                        b = e._time,
                        et = e._type,
                        i = e._value,
                        f = e._floatValue
                    }
                );
            }
        }
        converted.rotationEvents = rotationEvents.ToArray();
        converted.basicBeatMapEvents = basicBeatmapEvents.ToArray();
        converted.colorBoostBeatMapEvents = colorBoostBeatmapEvents.ToArray();

        return converted;
    }
}


[Serializable] public struct NoteV2
{
    public float _time;
    public int _lineIndex;
    public int _lineLayer;
    public int _type;
    public int _cutDirection;
}


[Serializable] public struct SliderV2
{
    public int _colorType;
    public float _headTime;
    public int _headLineIndex;
    public int _headLineLayer;
    public float _headControlPointLengthMultiplier;
    public int _headCutDirection;
    public float _tailTime;
    public int _tailLineIndex;
    public int _tailLineLayer;
    public float _tailControlPointLengthMultiplier;
    public int _tailCutDirection;
    public int _sliderMidAnchorMode;
}


[Serializable] public struct ObstacleV2
{
    public float _time;
    public int _lineIndex;
    public int _type;
    public float _duration;
    public int _width;
}


[Serializable] public struct EventV2
{
    public float _time;
    public int _type;
    public int _value;
    public float _floatValue;
}