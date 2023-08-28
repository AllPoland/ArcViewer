using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class BeatmapDifficultyV2
{
    public string _version;
    public BeatmapNoteV2[] _notes;
    public BeatmapSliderV2[] _sliders;
    public BeatmapObstacleV2[] _obstacles;
    public BeatmapEventV2[] _events;
    //Waypoints ommitted

    public bool HasObjects => _notes.Length + _obstacles.Length
        + _sliders.Length + _events.Length > 0;


    public BeatmapDifficultyV2()
    {
        _version = "";
        _notes = new BeatmapNoteV2[0];
        _sliders = new BeatmapSliderV2[0];
        _obstacles = new BeatmapObstacleV2[0];
        _events = new BeatmapEventV2[0];
    }


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
        AddNulls();

        BeatmapDifficulty converted = new BeatmapDifficulty
        {
            version = "3.0.0"
        };

        List<BeatmapColorNote> colorNotes = new List<BeatmapColorNote>();
        List<BeatmapBombNote> bombNotes = new List<BeatmapBombNote>();
        foreach(BeatmapNoteV2 n in _notes)
        {
            if(n._customData?._fake ?? false)
            {
                //Ignore fake objects entirely since there's no way to handle them atm
                continue;
            }

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
            if(o._customData?._fake ?? false)
            {
                //Ignore fake objects entirely since there's no way to handle them atm
                continue;
            }

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
        List<BeatmapBpmEvent> bpmEvents = new List<BeatmapBpmEvent>();
        for(int i = 0; i < _events.Length; i++)
        {
            BeatmapEventV2 e = _events[i];
            if(e._type == 14 || e._type == 15)
            {
                //Rotation event
                rotationEvents.Add
                (
                    new BeatmapRotationEvent
                    {
                        b = e._time,
                        e = e._type - 14,  //Subtracting 14 from the type makes it line up with the expected 0 and 1 in V3 format
                        r = e._customData?._rotation ?? ValueToRotation[Math.Clamp(e._value, 0, 7)]
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
            else if(e._type == 100)
            {
                if(e._floatValue == null)
                {
                    Debug.LogWarning("V2 BPM event has no _floatValue!");
                }
                else
                {
                    bpmEvents.Add
                    (
                        new BeatmapBpmEvent
                        {
                            b = e._time,
                            m = (float)e._floatValue
                        }
                    );
                }
            }
            else
            {
                //Other event
                BeatmapBasicBeatmapEvent newEvent = new BeatmapBasicBeatmapEvent
                {
                    b = e._time,
                    et = e._type,
                    i = e._value,
                    f = e._floatValue ?? 1f,
                    customData = e._customData?.ConvertToV3() ?? null
                };
                basicBeatmapEvents.Add(newEvent);

                if(e._customData?._lightGradient != null)
                {
                    //Light gradients need to be converted to transition events
                    BeatmapChromaGradientV2 gradient = e._customData._lightGradient;

                    //The v3 event will always have custom data if the v2 event does
                    newEvent.customData.color = gradient._startColor;
                    newEvent.customData.easing = gradient._easing;

                    float endBeat = e._time + gradient._duration;
                    int nextEventIndex = Array.FindIndex(_events, i, x =>
                        x._type == e._type
                        && (
                            //This technically leads to a rare issue where one lightID might have to end early
                            //but another doesn't, and both will end early anyway
                            //It's my belief that this issue is so rare and unnoticeable, and is
                            //due to lighter error so it doesn't matter
                            x._customData?._lightID == null
                            || x._customData._lightID.Any(y => e._customData._lightID.Contains(y))
                        )
                    );
        
                    if(nextEventIndex >= 0 && endBeat >= _events[nextEventIndex]._time)
                    {
                        //Don't allow the transition to overlap with the next event
                        endBeat = _events[nextEventIndex]._time - 0.001f;
                    }

                    BeatmapBasicBeatmapEvent newTransitionEvent = new BeatmapBasicBeatmapEvent
                    {
                        b = e._time + gradient._duration,
                        et = e._type,
                        i = 4,    //Event type 4 is a transition event - color doesn't matter
                        f = 1f,
                        customData = new BeatmapCustomBasicEventData
                        {
                            color = gradient._endColor,
                            lightID = e._customData._lightID
                        }
                    };
                    basicBeatmapEvents.Add(newTransitionEvent);
                }
            }
        }
        converted.rotationEvents = rotationEvents.ToArray();
        converted.basicBeatMapEvents = basicBeatmapEvents.ToArray();
        converted.colorBoostBeatMapEvents = colorBoostBeatmapEvents.ToArray();
        converted.bpmEvents = bpmEvents.ToArray();

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

    public BeatmapCustomEventDataV2 _customData;
}


[Serializable]
public class BeatmapCustomObjectDataV2
{
    public float[] _position;
    public float[] _color;
    public bool? _fake;


    public BeatmapCustomObjectData ConvertToV3()
    {
        return new BeatmapCustomObjectData
        {
            coordinates = _position,
            color = _color
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
            color = _color,
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
            color = _color,
            size = _scale
        };
    }
}


[Serializable]
public class BeatmapCustomEventDataV2
{
    [JsonConverter(typeof(LightIDConverter))]
    public int[] _lightID;
    public float[] _color;
    public string _easing;
    public string _lerpType;

    //Laser speed specific data
    public bool? _lockPosition;

    //Ring specific data
    public string _nameFilter;
    public float? _rotation;
    public float? _step;
    public float? _prop;

    //Shared by both rings and lasers
    public float? _speed;
    public float? _preciseSpeed;
    public int? _direction;

    public BeatmapChromaGradientV2 _lightGradient;


    public BeatmapCustomBasicEventData ConvertToV3()
    {
        return new BeatmapCustomBasicEventData
        {
            lightID = _lightID,
            color = _color,
            easing = _easing,
            lerpType = _lerpType,
            lockRotation = _lockPosition,
            nameFilter = _nameFilter,
            rotation = _rotation,
            step = _step,
            prop = _prop,
            speed = _preciseSpeed ?? _speed,
            direction = _direction
        };
    }
}


public class BeatmapChromaGradientV2
{
    public float _duration;
    public float[] _startColor;
    public float[] _endColor;
    public string _easing;
}