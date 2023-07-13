using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static MapElementList<ScoringEvent> ScoringEvents = new MapElementList<ScoringEvent>();

    public static int TotalScore => ScoringEvents.Count > 0 ? ScoringEvents.Last().TotalScore : 0;

    public static readonly byte[] ComboMultipliers = new byte[]
    {
        1,
        2,
        4,
        8
    };
    public static readonly byte[] HitsNeededForComboIncrease = new byte[]
    {
        2,
        4,
        8
    };


    private static int GetAccScoreFromCenterDistance(float centerDistance)
    {
        const int maxAccScore = 15;
        return Mathf.RoundToInt(maxAccScore * (1f - Mathf.Clamp01(centerDistance / 0.3f)));
    }


    public static int GetNoteScore(ScoringType type, float preSwingAmount, float postSwingAmount, float centerDistance)
    {
        const int preSwingValue = 70;
        const int postSwingValue = 30;

        if(type == ScoringType.ChainLink)
        {
            return 20;
        }
        
        if(type == ScoringType.ArcHead)
        {
            //Arc heads get post swing for free
            postSwingAmount = 1f;
        }
        else if(type == ScoringType.ArcTail)
        {
            //Arc tails get pre swing for free
            preSwingAmount = 1f;
        }
        else if(type == ScoringType.ChainHead)
        {
            //Chain heads don't get post swing points at all
            postSwingAmount = 0f;
        }

        int preSwingScore = Mathf.RoundToInt(Mathf.Clamp01(preSwingAmount) * preSwingValue);
        int postSwingScore = Mathf.RoundToInt(Mathf.Clamp01(postSwingAmount) * postSwingValue);
        return preSwingScore + postSwingScore + GetAccScoreFromCenterDistance(Mathf.Abs(centerDistance));
    }


    public static void InitializeScoringEvents()
    {
        ScoringEvents.SortElementsByBeat();

        int currentScore = 0;

        int combo = 0;
        byte comboMult = 0;
        byte comboProgress = 0;

        for(int i = 0; i < ScoringEvents.Count; i++)
        {
            ScoringEvent currentEvent = ScoringEvents[i];

            if(currentEvent.scoringType == ScoringType.Ignore)
            {
                continue;
            }

            bool isBadHit = currentEvent.noteEventType == NoteEventType.bad || currentEvent.noteEventType == NoteEventType.miss;
            if(isBadHit || currentEvent.scoringType == ScoringType.NoScore)
            {
                combo = 0;
                if(comboMult > 0)
                {
                    comboMult--;
                }
                comboProgress = 0;
            }
            else
            {
                combo++;
                if(comboMult < ComboMultipliers.Length - 1)
                {
                    //Combo is below max, and should be incremented
                    comboProgress++;
                    if(comboProgress >= HitsNeededForComboIncrease[comboMult])
                    {
                        //Combo multiplier has increased
                        comboMult++;
                        comboProgress = 0;
                    }
                }

                currentScore += currentEvent.ScoreGained * ComboMultipliers[comboMult];
            }

            currentEvent.TotalScore = currentScore;
            currentEvent.Combo = combo;
            currentEvent.ComboMult = comboMult;
            currentEvent.ComboProgress = comboProgress;

            Debug.Log($"Event #{i} | Time: {System.Math.Round(currentEvent.Time, 2)} | Type: {currentEvent.scoringType} | Score: {currentEvent.ScoreGained} | Total score: {currentScore} | Combo: {combo} | Combo mult: {ComboMultipliers[comboMult]}x");
        }

        Debug.Log($"Initialized Scoring Events for replay with total score: {TotalScore}");
    }


    public static void AddNoteScoringEvent(ScoringType scoringType, NoteEventType eventType, float time, float xPos, NoteCutInfo cutInfo)
    {
        if(eventType == NoteEventType.good && cutInfo == null)
        {
            throw new System.ArgumentNullException("A good cut cannot have null cutInfo!");
        }

        if(scoringType == ScoringType.Ignore || scoringType == ScoringType.NoScore)
        {
            throw new System.ArgumentException("The event must be a positive ScoringType!");
        }

        ScoringEvent newEvent = new ScoringEvent();
        newEvent.scoringType = scoringType;
        newEvent.noteEventType = eventType;
        newEvent.Time = time;
        newEvent.xPos = xPos;

        if(eventType == NoteEventType.good)
        {
            newEvent.ScoreGained = GetNoteScore(scoringType, cutInfo.beforeCutRating, cutInfo.afterCutRating, cutInfo.cutDistanceToCenter);
        }

        ScoringEvents.Add(newEvent);
    }


    private void UpdateReplay(Replay newReplay)
    {
        ScoringEvents.Clear();
    }


    private void UpdateReplayMode(bool replayMode)
    {
        if(!replayMode)
        {
            ScoringEvents.Clear();
        }
    }


    private void UpdateUIState(UIState newState)
    {
        ScoringEvents.Clear();
    }


    private void Start()
    {
        ReplayManager.OnReplayUpdated += UpdateReplay;
        ReplayManager.OnReplayModeChanged += UpdateReplayMode;

        UIStateManager.OnUIStateChanged += UpdateUIState;
    }
}


public class ScoringEvent : MapElement
{
    public ScoringType scoringType;
    public NoteEventType noteEventType;
    public int ScoreGained;
    public int TotalScore;

    public float xPos;

    public int Combo;
    public int ComboMult;
    public byte ComboProgress;
}