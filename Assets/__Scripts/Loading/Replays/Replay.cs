using System.Collections.Generic;
using UnityEngine;

public class Replay
{
    public ReplayInfo info = new ReplayInfo();

    public List<Frame> frames = new List<Frame>();

    public List<NoteEvent> notes = new List<NoteEvent>();
    public List<WallEvent> walls = new List<WallEvent>();
    public List<AutomaticHeight> heights = new List<AutomaticHeight>();
    public List<Pause> pauses = new List<Pause>();
    public SaberOffsets saberOffsets = new SaberOffsets();
    public Dictionary<string, byte[]> customData = new Dictionary<string, byte[]>();

    public List<LegacyScoreFrame> scoreSaberLegacyScoreData;
    public bool scoreSaberLegacyConverted;
    public LegacyHUDData scoreSaberLegacyHUDData;
}


public struct LegacyScoreFrame
{
    public float time;
    public int score;
    public int combo;
}

public class ReplayInfo
{
    public static string DifficultyNameFromRaw(int raw) => raw switch
    {
        1 => "Easy", 3 => "Normal", 5 => "Hard",
        7 => "Expert", 9 => "ExpertPlus", _ => "Expert"
    };

    public string version;
    public string gameVersion;
    public string timestamp;

    public string playerID;
    public string playerName;
    public string platform;

    public string trackingSytem;
    public string hmd;
    public string controller;

    public string hash;
    public string songName;
    public string mapper;
    public string difficulty;

    public int score;
    public string mode;
    public string environment;
    public string modifiers;
    public float jumpDistance;
    public bool leftHanded;
    public float height;

    public float startTime;
    public float failTime;
    public float speed;
}

public class Frame
{
    public float time;
    public int fps;
    public PositionData head;
    public PositionData leftHand;
    public PositionData rightHand;
};

public enum NoteEventType
{
    good = 0,
    bad = 1,
    miss = 2,
    bomb = 3
}

public class NoteEvent
{
    public int noteID;
    public float eventTime;
    public float spawnTime;
    public NoteEventType eventType;
    public NoteCutInfo noteCutInfo;

    public int lineIndex = -1;
    public int lineLayer = -1;
    public int colorType = -1;
    public int cutDirection = -1;
    public int noteScoringType = -1;
};

public class WallEvent
{
    public int wallID;
    public float energy;
    public float time;
    public float spawnTime;
};

public class AutomaticHeight
{
    public float height;
    public float time;
};

public class Pause
{
    public long duration;
    public float time;
};

public class NoteCutInfo
{
    public bool speedOK;
    public bool directionOK;
    public bool saberTypeOK;
    public bool wasCutTooSoon;
    public float saberSpeed;
    public Vector3 saberDir;
    public int saberType;
    public float timeDeviation;
    public float cutDirDeviation;
    public Vector3 cutPoint;
    public Vector3 cutNormal;
    public float cutDistanceToCenter;
    public float cutAngle;
    public float beforeCutRating;
    public float afterCutRating;
};

public class SaberOffsets
{
    public Vector3 LeftSaberLocalPosition;
    public Quaternion LeftSaberLocalRotation;
    public Vector3 RightSaberLocalPosition;
    public Quaternion RightSaberLocalRotation;
}

public enum StructType
{
    info = 0,
    frames = 1,
    notes = 2,
    walls = 3,
    heights = 4,
    pauses = 5,
    saberOffsets = 6,
    customData = 7
}

public class PositionData
{
    public Vector3 position;
    public Quaternion rotation;
}