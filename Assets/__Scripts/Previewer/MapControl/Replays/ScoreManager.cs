using System;
using System.Globalization;
using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static MapElementList<ScoringEvent> ScoringEvents = new MapElementList<ScoringEvent>();

    public static int MaxScore { get; private set; }
    public static int TotalScore => ScoringEvents.Count > 0 ? ScoringEvents.Last().TotalScore : 0;

    public const int MaxNoteScore = 115;
    public const int MaxChainHeadScore = 85;
    public const int MaxChainLinkScore = 20;

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

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI scorePercentageText;
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI missText;


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
            return MaxChainLinkScore;
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
        int maxScore = 0;

        int combo = 0;
        byte comboMult = 0;
        byte comboProgress = 0;

        int fcComboMult = 0;
        int fcComboProgress = 0;

        int misses = 0;

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

                misses++;
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

            if(fcComboMult < ComboMultipliers.Length - 1)
            {
                //Increment FC combo, which is used to calculate max score
                fcComboProgress++;
                if(fcComboProgress >= HitsNeededForComboIncrease[fcComboMult])
                {
                    fcComboMult++;
                    fcComboProgress = 0;
                }
            }

            switch(currentEvent.scoringType)
            {
                case ScoringType.Note:
                case ScoringType.ArcHead:
                case ScoringType.ArcTail:
                    maxScore += MaxNoteScore * ComboMultipliers[fcComboMult];
                    break;
                case ScoringType.ChainHead:
                    maxScore += MaxChainHeadScore * ComboMultipliers[fcComboMult];
                    break;
                case ScoringType.ChainLink:
                    maxScore += MaxChainLinkScore * ComboMultipliers[fcComboMult];
                    break;
            }

            currentEvent.TotalScore = currentScore;
            currentEvent.MaxScore = maxScore;
            currentEvent.Combo = combo;
            currentEvent.ComboMult = comboMult;
            currentEvent.ComboProgress = comboProgress;
            currentEvent.Misses = misses;

            currentEvent.ScorePercentage = ((float)currentScore / maxScore) * 100;

            Debug.Log($"Event #{i} | Time: {Math.Round(currentEvent.Time, 2)} | Type: {currentEvent.scoringType} | Score: {currentEvent.ScoreGained} | Total score: {currentScore} | Max score: {maxScore} | Combo: {combo} | Combo mult: {ComboMultipliers[comboMult]}x");
        }

        Debug.Log($"Initialized Scoring Events for replay with total score: {TotalScore}");
    }


    public static void AddNoteScoringEvent(ScoringType scoringType, NoteEventType eventType, float time, float xPos, NoteCutInfo cutInfo)
    {
        if(eventType == NoteEventType.good && cutInfo == null)
        {
            throw new ArgumentNullException("A good cut cannot have null cutInfo!");
        }

        if(scoringType == ScoringType.Ignore || scoringType == ScoringType.NoScore)
        {
            throw new ArgumentException("The event must be a positive ScoringType!");
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


    private static string GradeFromPercentage(float percentage)
    {
        if(percentage >= 90)
        {
            return "SS";
        }
        else if(percentage >= 80)
        {
            return "S";
        }
        else if(percentage >= 65)
        {
            return "A";
        }
        else if(percentage >= 50)
        {
            return "B";
        }
        else if(percentage >= 35)
        {
            return "C";
        }
        else if(percentage >= 20)
        {
            return "D";
        }
        else return "E";
    }


    private void UpdateScoreTexts(int lastEventIndex)
    {

    }


    private void UpdateBeat(float beat)
    {
        int lastIndex = ScoringEvents.GetLastIndex(TimeManager.CurrentTime, x => x.Time <= TimeManager.CurrentTime);

        int currentScore;
        float currentPercentage;
        int currentCombo;
        int currentComboMult;
        int currentComboProgress;
        int currentMisses;
        if(lastIndex >= 0)
        {
            ScoringEvent lastEvent = ScoringEvents[lastIndex];

            currentScore = lastEvent.TotalScore;
            currentPercentage = lastEvent.ScorePercentage;
            currentCombo = lastEvent.Combo;
            currentComboMult = lastEvent.ComboMult;
            currentComboProgress = lastEvent.ComboProgress;
            currentMisses = lastEvent.Misses;

            UpdateScoreTexts(lastIndex);
        }
        else
        {
            currentScore = 0;
            currentPercentage = 100f;
            currentCombo = 0;
            currentComboMult = 0;
            currentComboProgress = 0;
            currentMisses = 0;
        }

        comboText.text = currentCombo.ToString();
        missText.text = currentMisses.ToString();

        gradeText.text = GradeFromPercentage(currentPercentage);

        //The score gets a space inserted between every 3 decimals
        string baseScoreString = currentScore.ToString();
        string scoreString = "";
        int maxIndex = baseScoreString.Length;
        for(int i = maxIndex - 1; i >= 0; i -= 3)
        {
            //Gather the next digits, up to 3 if they're available
            //(logic is funky cause this needs to be done backwards)
            int startIndex = Mathf.Max(i - 2, 0);
            int length = Mathf.Min(3, maxIndex - startIndex);
            string substring = baseScoreString.Substring(startIndex, length);
            if(scoreString == "")
            {
                //No space if the string is empty
                scoreString = substring;
            }
            else scoreString = substring + ' ' + scoreString;

            //Make sure none of these digits are used by the next round
            maxIndex = startIndex;
        }

        scoreText.text = scoreString;

        float roundedPercentage = (float)Math.Round(currentPercentage, 2);
        string percentageString = roundedPercentage.ToString(CultureInfo.InvariantCulture);
        string[] split = percentageString.Split('.');
        int decimals = split.Length > 1 ? split[1].Length : 0;

        if(decimals == 0)
        {
            percentageString = $"{percentageString}.00%";
        }
        else if(decimals == 1)
        {
            percentageString = $"{percentageString}0%";
        }
        else percentageString = $"{percentageString}%";

        scorePercentageText.text = percentageString;
    }


    private void Reset()
    {
        ScoringEvents.Clear();
        TimeManager.OnBeatChanged -= UpdateBeat;
    }


    private void UpdateReplay(Replay newReplay)
    {
        UpdateReplayMode(ReplayManager.IsReplayMode);
    }


    private void UpdateReplayMode(bool replayMode)
    {
        if(replayMode)
        {
            ScoringEvents.Clear();
            TimeManager.OnBeatChanged += UpdateBeat;
        }
        else
        {
            Reset();
        }
    }


    private void UpdateUIState(UIState newState)
    {
        if(newState != UIState.Previewer)
        {
            Reset();
        }
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
    public int MaxScore;
    public float ScorePercentage;

    public float xPos;

    public int Combo;
    public int ComboMult;
    public byte ComboProgress;

    public int Misses;
}