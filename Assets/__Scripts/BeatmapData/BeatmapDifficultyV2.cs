using System;
using System.Collections.Generic;

[Serializable]
public class BeatmapDifficultyV2
{
    public string _version;
    public BeatmapNoteV2[] _notes;
    public BeatmapSliderV2[] _sliders;
    public BeatmapObstacleV2[] _obstacles;
    public BeatmapEventV2[] _events;
    //Waypoints ommitted


    public BeatmapDifficulty ConvertToV3()
    {
        BeatmapDifficulty converted = new BeatmapDifficulty
        {
            version = "3.0.0"
        };

        List<BeatmapColorNote> colorNotes = new List<BeatmapColorNote>();
        List<BeatmapBombNote> bombNotes = new List<BeatmapBombNote>();
        foreach(BeatmapNoteV2 n in _notes)
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
                    new BeatmapColorNote
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
                    new BeatmapBombNote
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


        List<BeatmapObstacle> obstacles = new List<BeatmapObstacle>();
        foreach(BeatmapObstacleV2 o in _obstacles)
        {
            obstacles.Add
            (
                new BeatmapObstacle
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


        List<BeatmapSlider> sliders = new List<BeatmapSlider>();
        foreach(BeatmapSliderV2 s in _sliders)
        {
            sliders.Add
            (
                new BeatmapSlider
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


        List<BeatmapRotationEvent> rotationEvents = new List<BeatmapRotationEvent>();
        List<BeatmapBasicBeatmapEvent> basicBeatmapEvents = new List<BeatmapBasicBeatmapEvent>();
        List<BeatmapColorBoostBeatmapEvent> colorBoostBeatmapEvents = new List<BeatmapColorBoostBeatmapEvent>();
        foreach(BeatmapEventV2 e in _events)
        {
            if(e._type == 14 || e._type == 15)
            {
                //Rotation event
                rotationEvents.Add
                (
                    new BeatmapRotationEvent
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
                    new BeatmapColorBoostBeatmapEvent
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
                    new BeatmapBasicBeatmapEvent
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


[Serializable]
public struct BeatmapNoteV2
{
    public float _time;
    public int _lineIndex;
    public int _lineLayer;
    public int _type;
    public int _cutDirection;

    public BeatmapCustomObjectDataV2 _customData;
}


[Serializable]
public struct BeatmapSliderV2
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


[Serializable]
public struct BeatmapObstacleV2
{
    public float _time;
    public int _lineIndex;
    public int _type;
    public float _duration;
    public int _width;

    public BeatmapCustomObstacleDataV2 _customData;
}


[Serializable]
public struct BeatmapEventV2
{
    public float _time;
    public int _type;
    public int _value;
    public float _floatValue;
}


[Serializable]
public class BeatmapCustomObjectDataV2
{
    public float[] _position;


    public static BeatmapCustomObjectData ConvertToV3(BeatmapCustomObstacleDataV2 cd)
    {
        return new BeatmapCustomObjectData
        {
            coordinates = cd._position
        };
    }
}


[Serializable]
public class BeatmapCustomObstacleDataV2 : BeatmapCustomObjectDataV2
{
    public float[] _scale;


    public static new BeatmapCustomObstacleData ConvertToV3(BeatmapCustomObstacleDataV2 cd)
    {
        return new BeatmapCustomObstacleData
        {
            coordinates = cd._position,
            size = cd._scale
        };
    }
}