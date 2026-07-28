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

        // ScoreSaber-specific logic. This should probably get a clearer definition later
        if(noteEvent.lineIndex >= 0)
        {
            int st = noteEvent.noteScoringType;
            int c = noteEvent.colorType;
            int d = noteEvent.cutDirection;
            if(noteEvent.eventType == NoteEventType.bomb)
            {
                st = (int)ScoringType.NoScore;
                c = 3;
                d = 3;
            }
            ID = st * 10000 + noteEvent.lineIndex * 1000 + noteEvent.lineLayer * 100 + c * 10 + d;
        }
        else
        {
            // BSOR V1 logic
            ID = noteEvent.noteID;
        }

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
            ScoreGained = ScoringUtils.MaxChainLinkScore;
            PreSwingScore = 0;
            PostSwingScore = 0;
            MaxSwingScore = ScoringUtils.MaxChainLinkScore;
            return;
        }
        
        if(scoringType == ScoringType.ArcHead)
        {
            //Arc heads get post swing for free
            PostSwingAmount = 1f;
            MaxSwingScore = ScoringUtils.MaxNoteScore;
        }
        else if(scoringType == ScoringType.ArcTail)
        {
            //Arc tails get pre swing for free
            PreSwingAmount = 1f;
            MaxSwingScore = ScoringUtils.MaxNoteScore;
        }
        else if(scoringType == ScoringType.ArcHeadArcTail || scoringType == ScoringType.ChainHeadArcTail)
        {
            //Arc head/tails get both pre and post swing for free
            PreSwingAmount = 1f;
            PostSwingAmount = 1f;
            MaxSwingScore = ScoringUtils.MaxNoteScore;
        }
        else if(scoringType == ScoringType.ChainHead
            || scoringType == ScoringType.ChainHeadArcHead
            || scoringType == ScoringType.ChainHeadArcHeadArcTail)
        {
            //Chain heads don't get post swing points at all
            PostSwingAmount = 0f;
            MaxSwingScore = ScoringUtils.MaxChainHeadScore;
        }
        else MaxSwingScore = ScoringUtils.MaxNoteScore;

        PreSwingScore = Mathf.RoundToInt(Mathf.Clamp01(PreSwingAmount) * ScoringUtils.PreSwingValue);
        PostSwingScore = Mathf.RoundToInt(Mathf.Clamp01(PostSwingAmount) * ScoringUtils.PostSwingValue);
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


    public static ScoringEvent MatchNote(List<ScoringEvent> scoringEvents, ScoringType scoringType, int noteID)
    {
        ScoringType originalType = scoringType;

        int noTypeID = noteID - (int)scoringType * 10000;

        foreach(ScoringType replayScoringType in CompatibleReplayScoringTypes(scoringType))
        {
            int replayNoteID = noTypeID + (int)replayScoringType * 10000;
            ScoringEvent scoringEvent = scoringEvents.Find(x => x.ID == replayNoteID);
            if(scoringEvent != null)
            {
                return scoringEvent;
            }
        }

        if(scoringType == ScoringType.Note)
        {
            //older replay ids can omit the scoring type for normal notes
            return scoringEvents.Find(x => x.ID == noTypeID);
        }

        return null;
    }


    private static IEnumerable<ScoringType> CompatibleReplayScoringTypes(ScoringType scoringType)
    {
        // same aliases as the scoresaber pc mod
        switch(scoringType)
        {
            case ScoringType.ArcHeadArcTail:
                yield return ScoringType.ArcHeadArcTail;
                yield return ScoringType.ArcHead;
                yield return ScoringType.ArcTail;
                break;

            case ScoringType.ChainHeadArcHead:
                yield return ScoringType.ChainHeadArcHead;
                yield return ScoringType.ArcHead;
                break;

            case ScoringType.ChainHeadArcTail:
                yield return ScoringType.ChainHeadArcTail;
                yield return ScoringType.ChainHead;
                yield return ScoringType.ArcTail;
                break;

            case ScoringType.ChainHeadArcHeadArcTail:
                yield return ScoringType.ChainHeadArcHeadArcTail;
                yield return ScoringType.ChainHeadArcTail;
                yield return ScoringType.ArcHead;
                break;

            case ScoringType.ChainLinkArcHead:
                yield return ScoringType.ChainLinkArcHead;
                yield return ScoringType.ChainLink;
                yield return ScoringType.ArcHead;
                break;

            default:
                yield return scoringType;
                break;
        }
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
    ChainLinkArcHead = 10,
    ChainHeadArcHead = 11,
    ChainHeadArcHeadArcTail = 12
}