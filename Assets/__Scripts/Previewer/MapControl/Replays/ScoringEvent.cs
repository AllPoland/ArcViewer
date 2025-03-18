using System.Collections.Generic;
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
    public int PreSwingScore;
    public int PostSwingScore;
    public int AccuracyScore;
    public int MaxSwingScore;
    public float TimeDependency;

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
    public ScoreTextInfo textInfo;

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
            TimeDependency = Mathf.Abs(noteEvent.noteCutInfo.cutNormal.z);
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


    private static int GetAccScoreFromCenterDistance(float centerDistance)
    {
        const int maxAccScore = 15;
        return Mathf.RoundToInt(maxAccScore * (1f - Mathf.Clamp01(centerDistance / 0.3f)));
    }


    private void CalculateNoteScore()
    {
        if(scoringType == ScoringType.ChainLink || scoringType == ScoringType.ChainLinkArcHead)
        {
            ScoreGained = ScoreManager.MaxChainLinkScore;
            PreSwingScore = 0;
            PostSwingScore = 0;
            MaxSwingScore = ScoreManager.MaxChainLinkScore;
            return;
        }
        
        if(scoringType == ScoringType.ArcHead)
        {
            //Arc heads get post swing for free
            PostSwingAmount = 1f;
            MaxSwingScore = ScoreManager.MaxNoteScore;
        }
        else if(scoringType == ScoringType.ArcTail)
        {
            //Arc tails get pre swing for free
            PreSwingAmount = 1f;
            MaxSwingScore = ScoreManager.MaxNoteScore;
        }
        else if(scoringType == ScoringType.ArcHeadArcTail)
        {
            //Arc head/tails get both pre and post swing for free
            PreSwingAmount = 1f;
            PostSwingAmount = 1f;
            MaxSwingScore = ScoreManager.MaxNoteScore;
        }
        else if(scoringType == ScoringType.ChainHead || scoringType == ScoringType.ChainHeadArcTail)
        {
            //Chain heads don't get post swing points at all
            PostSwingAmount = 0f;
            MaxSwingScore = ScoreManager.MaxChainHeadScore;
        }
        else MaxSwingScore = ScoreManager.MaxNoteScore;

        PreSwingScore = Mathf.RoundToInt(Mathf.Clamp01(PreSwingAmount) * ScoreManager.PreSwingValue);
        PostSwingScore = Mathf.RoundToInt(Mathf.Clamp01(PostSwingAmount) * ScoreManager.PostSwingValue);
        AccuracyScore = GetAccScoreFromCenterDistance(Mathf.Abs(SwingCenterDistance));
        ScoreGained = PreSwingScore + PostSwingScore + AccuracyScore;
    }


    public void SetEventValues(ScoringType newScoringType, Vector2 newPosition)
    {
        const float scoreIndicatorXRandomness = 0.15f;

        scoringType = newScoringType;
        position = newPosition;

        endX = position.x + UnityEngine.Random.Range(-scoreIndicatorXRandomness, scoreIndicatorXRandomness);

        if(noteEventType == NoteEventType.good)
        {
            CalculateNoteScore();
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
        if(ID >= 110000 || colorType > 10 || cutDirection > 8)
        {
            //In ME, the ID can become worthless for identifying note type and position
            //so we can only tell whether this was a bomb or a note
            SetEventValues(isBomb ? ScoringType.NoScore : ScoringType.Note, Vector2.zero);
            return;
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


    public static ScoringEvent BruteForceMatchNote(List<ScoringEvent> scoringEvents, ref ScoringType scoringType, int noteID, bool hasTail, bool hasHead, bool isChainHead)
    {
        int noTypeID = noteID - (int)scoringType * 10000;
        if(scoringType == ScoringType.Note)
        {
            //Note scoringType can also count as 0 sometimes (very scuffed)
            noteID = noTypeID;
            return scoringEvents.Find(x => x.ID == noteID);
        }
        
        ScoringEvent scoringEvent;
        if(scoringType == ScoringType.ArcHeadArcTail)
        {
            if(isChainHead)
            {
                //Try ChainHeadArcTail
                noteID = noTypeID + (int)ScoringType.ChainHeadArcTail * 10000;
                scoringEvent = scoringEvents.Find(x => x.ID == noteID);
                if(scoringEvent != null)
                {
                    scoringType = ScoringType.ChainHeadArcTail;
                    return scoringEvent;
                }

                //Try just chain head
                noteID = noTypeID + (int) ScoringType.ChainHead * 10000;
                scoringEvent = scoringEvents.Find(x => x.ID == noteID);
                if(scoringEvent != null)
                {
                    scoringType = ScoringType.ChainHead;
                    return scoringEvent;
                }
            }

            //Replays pre-1.40 can only include head *or* tail
            noteID = noTypeID + (int)ScoringType.ArcHead * 10000;
            scoringEvent = scoringEvents.Find(x => x.ID == noteID);
            if(scoringEvent != null)
            {
                scoringType = ScoringType.ArcHead;
                return scoringEvent;
            }

            //If this last check fails, we'll return null regardless
            noteID = noTypeID + (int)ScoringType.ArcTail * 10000;
            scoringType = ScoringType.ArcTail;
            return scoringEvents.Find(x => x.ID == noteID);
        }

        if(scoringType == ScoringType.ChainHeadArcTail)
        {
            //Try just chain head
            noteID = noTypeID + (int) ScoringType.ChainHead * 10000;
            scoringEvent = scoringEvents.Find(x => x.ID == noteID);
            if(scoringEvent != null)
            {
                scoringType = ScoringType.ChainHead;
                return scoringEvent;
            }

            if(hasHead)
            {
                //Try just arc head
                noteID = noTypeID + (int)ScoringType.ArcHead * 10000;
                scoringEvent = scoringEvents.Find(x => x.ID == noteID);
                if(scoringEvent != null)
                {
                    scoringType = ScoringType.ArcHead;
                    return scoringEvent;
                }
            }

            //If this last check fails, we'll return null regardless
            noteID = noTypeID + (int)ScoringType.ArcTail * 10000;
            scoringType = ScoringType.ArcTail;
            return scoringEvents.Find(x => x.ID == noteID);
        }

        if(scoringType == ScoringType.ChainHead && hasHead)
        {
            //Try just arc head
            noteID = noTypeID + (int)ScoringType.ArcHead * 10000;
            scoringEvent = scoringEvents.Find(x => x.ID == noteID);
            if(scoringEvent != null)
            {
                scoringType = ScoringType.ArcHead;
                return scoringEvent;
            }
        }

        //Nothing matched
        return null;
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
    ChainLink = 7,
    ArcHeadArcTail = 8,
    ChainHeadArcTail = 9,
    ChainLinkArcHead = 10
}