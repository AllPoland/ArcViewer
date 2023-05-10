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


    public void AddNulls()
    {
        _version = _version ?? "2.6.0";
        _notes = _notes ?? new BeatmapNoteV2[0];
        _sliders = _sliders ?? new BeatmapSliderV2[0];
        _obstacles = _obstacles ?? new BeatmapObstacleV2[0];
        _events = _events ?? new BeatmapEventV2[0];
    }


    public static readonly Dictionary<int, float> ValueToRotation = new Dictionary<int, float>
    {
        {0, -60f},
        {1, -45f},
        {2, -30f},
        {3, -15f},
        {4, 15f},
        {5, 30f},
        {6, 45f},
        {7, 60f}
    };


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
            if(n._type == 0 || n._type == 1)
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
                        a = 0,
                        customData = n._customData?.ConvertToV3() ?? null
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
                        y = n._lineLayer,
                        customData = n._customData?.ConvertToV3() ?? null
                    }
                );
            }
        }
        converted.colorNotes = colorNotes.ToArray();
        converted.bombNotes = bombNotes.ToArray();


        List<BeatmapObstacle> obstacles = new List<BeatmapObstacle>();
        foreach(BeatmapObstacleV2 o in _obstacles)
        {
            int wallY = o._type == 0 ? 0 : 2;
            int wallH = o._type == 0 ? 5 : 3;
            if(o._type >= 1000)
            {
                //This is a mapping extensions wall
                if(o._type > 4000)
                {
                    //Extra precision walls are so weird wtf
                    int type = o._type - 4001;
                    int position = type % 1000;
                    int height = (type - position) / 1000;

                    //Position is supposedly 4x as much as height but that doesn't actually work?
                    //I need to figure this out at some point
                    wallY = (position * 5) + 1000;
                    wallH = (height * 5) + 1000;
                }
                else
                {
                    //Standard precision height
                    int height = o._type - 1000;
                    wallH = (height * 5) + 1000;
                    wallY = 0;
                }
            }

            obstacles.Add
            (
                new BeatmapObstacle
                {
                    b = o._time,
                    x = o._lineIndex,
                    y = wallY,
                    d = o._duration,
                    w = o._width,
                    h = wallH,
                    customData = o._customData?.ConvertToV3() ?? null
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
                        e = e._type - 14,  //Subtracting 14 from the type makes it line up with the expected 0 and 1 in V3 format
                        r = ValueToRotation[Math.Clamp(e._value, 0, 7)]
                    }
                );
            }
            else if(e._type == 5)
            {
                //Boost color event
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
                        f = e._floatValue ?? 1f
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

    public BeatmapCustomNoteDataV2 _customData;
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
    public float? _floatValue;
}


[Serializable]
public class BeatmapCustomObjectDataV2
{
    public float[] _position;


    public BeatmapCustomObjectData ConvertToV3()
    {
        return new BeatmapCustomObjectData
        {
            coordinates = _position
        };
    }
}


[Serializable]
public class BeatmapCustomNoteDataV2 : BeatmapCustomObjectDataV2
{
    public float? _cutDirection;


    public new BeatmapCustomNoteData ConvertToV3()
    {
        return new BeatmapCustomNoteData
        {
            coordinates = _position,
            angle = _cutDirection
        };
    }
}


[Serializable]
public class BeatmapCustomObstacleDataV2 : BeatmapCustomObjectDataV2
{
    public float[] _scale;


    public new BeatmapCustomObstacleData ConvertToV3()
    {
        return new BeatmapCustomObstacleData
        {
            coordinates = _position,
            size = _scale
        };
    }
}