using UnityEngine;

public class ScoringEvent : MapElement
{
    public bool Initialized;

    public int ID;
    public float ObjectTime;

    public bool IsWall;
    public float WallExitEnergy;

    public ScoringType scoringType;
    public NoteEventType noteEventType;

    public float PreSwingAmount;
    public float PostSwingAmount;
    public float SwingCenterDistance;
    public float HitTimeOffset;

    public int ScoreGained;
    public int TotalScore;
    public int FCScore;
    public int MaxScore;
    public int MaxScoreNoMisses;
    public float ScorePercentage;
    public float FCScorePercentage;

    public Vector2 position;
    public float endX;

    public int Combo;
    public int ComboMult;
    public byte ComboProgress;

    public int Misses;

    public ScoreIndicatorHandler visual;

    public bool IsBadHit => noteEventType == NoteEventType.bad
        || noteEventType == NoteEventType.miss
        || scoringType == ScoringType.NoScore
        || noteEventType == NoteEventType.bomb;


    public ScoringEvent(NoteEvent noteEvent)
    {
        Initialized = false;

        ID = noteEvent.noteID;
        IsWall = false;
        Time = noteEvent.eventTime;
        ObjectTime = noteEvent.spawnTime;
        noteEventType = noteEvent.eventType;

        if(noteEvent.noteCutInfo != null)
        {
            PreSwingAmount = noteEvent.noteCutInfo.beforeCutRating;
            PostSwingAmount = noteEvent.noteCutInfo.afterCutRating;
            SwingCenterDistance = noteEvent.noteCutInfo.cutDistanceToCenter;
            HitTimeOffset = noteEvent.noteCutInfo.timeDeviation;
        }
    }


    public ScoringEvent(WallEvent wallEvent)
    {
        Initialized = false;

        ID = wallEvent.wallID;
        IsWall = true;
        Time = wallEvent.time;
        ObjectTime = wallEvent.spawnTime;
        noteEventType = NoteEventType.bad;
        scoringType = ScoringType.NoScore;
        WallExitEnergy = wallEvent.energy;
    }


    public void SetEventValues(ScoringType newScoringType, Vector2 newPosition)
    {
        const float scoreIndicatorXRandomness = 0.15f;

        scoringType = newScoringType;
        position = newPosition;

        endX = position.x + UnityEngine.Random.Range(-scoreIndicatorXRandomness, scoreIndicatorXRandomness);

        if(noteEventType == NoteEventType.good)
        {
            ScoreGained = ScoreManager.GetNoteScore(scoringType, PreSwingAmount, PostSwingAmount, SwingCenterDistance);
        }

        Initialized = true;
    }


    public void InferEventValues()
    {
        int noteID = ID;

        int cutDirection = noteID % 10;
        noteID -= cutDirection;
        int colorType = noteID % 100;
        noteID -= colorType;
        colorType /= 10;

        bool isBomb = noteEventType == NoteEventType.bomb;
        if(ID >= 80000 || colorType > 10 || cutDirection > 8)
        {
            //In ME, the ID can become worthless for identifying note type and position
            //so we can only tell whether this was a bomb or a note
            SetEventValues(isBomb ? ScoringType.NoScore : ScoringType.Note, Vector2.zero);
        }

        int y = noteID % 1000;
        noteID -= y;
        y /= 100;
        int x = noteID % 10000;
        noteID -= x;
        x /= 1000;
        int type = noteID / 10000;

        Vector2 newPosition = ObjectManager.CalculateObjectPosition(x, y);
        newPosition = ObjectManager.Instance.ObjectSpaceToWorldSpace(newPosition);

        SetEventValues((ScoringType)type, newPosition);
    }
}


public enum ScoringType
{
    Ignore = 1,
    NoScore = 2,
    Note = 3,
    ArcHead = 4,
    ArcTail = 5,
    ChainHead = 6,
    ChainLink = 7
}