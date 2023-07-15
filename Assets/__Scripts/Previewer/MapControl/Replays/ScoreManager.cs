using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static MapElementList<ScoringEvent> ScoringEvents = new MapElementList<ScoringEvent>();
    private static List<ScoringEvent> RenderedScoringEvents = new List<ScoringEvent>();

    public static int MaxScore { get; private set; }
    public static int TotalScore => ScoringEvents.Count > 0 ? ScoringEvents.Last().TotalScore : 0;

    public const int MaxNoteScore = 115;
    public const int MaxChainHeadScore = 85;
    public const int MaxChainLinkScore = 20;

    private const float scoreIndicatorXRandomness = 0.15f;

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
        8,
        255
    };

    [Header("Components")]
    [SerializeField] private GameObject hudObject;

    [Space]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI scorePercentageText;
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI missText;

    [Space]
    [SerializeField] private TextMeshProUGUI multiplierText;
    [SerializeField] private Image comboProgressFill;

    [Space]
    [SerializeField] private TMProPool scoreIndicatorPool;
    [SerializeField] private Transform scoreIndicatorParent;

    [Header("Parameters")]
    [SerializeField] private string multiplierPrefix;

    [Space]
    [SerializeField] private float indicatorStartZ;
    [SerializeField] private float indicatorEndY;
    [SerializeField] private float indicatorEndZ;
    [SerializeField] private float indicatorLifetime;
    [SerializeField] private float indicatorFadeInTime;
    [SerializeField] private float indicatorFadeOutTime;

    [Space]
    [SerializeField] private float badEndY;
    [SerializeField] private float badEndZ;
    [SerializeField] private string badCutString;
    [SerializeField] private string missString;

    [Header("Colors")]
    [SerializeField] private Color badColor = Color.white;


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

            currentEvent.ScorePercentage = maxScore == 0 ? 100f : ((float)currentScore / maxScore) * 100;
            // Debug.Log($"Event #{i} | Time: {Math.Round(currentEvent.Time, 2)} | Type: {currentEvent.scoringType} | Score: {currentEvent.ScoreGained} | Total score: {currentScore} | Max score: {maxScore} | Combo: {combo} | Combo mult: {ComboMultipliers[comboMult]}x");
        }

        Debug.Log($"Initialized Scoring Events for replay with total score: {TotalScore}");
    }


    public static void AddNoteScoringEvent(ScoringType scoringType, NoteEventType eventType, float time, Vector2 position, NoteCutInfo cutInfo)
    {
        if(eventType == NoteEventType.good && cutInfo == null)
        {
            throw new ArgumentNullException("A good cut cannot have null cutInfo!");
        }

        if(scoringType == ScoringType.Ignore)
        {
            return;
        }

        ScoringEvent newEvent = new ScoringEvent();
        newEvent.scoringType = scoringType;
        newEvent.noteEventType = eventType;
        newEvent.Time = time;
        newEvent.position = position;

        //Y position needs to be adjusted to worldspace, and the endX gets randomized a bit
        newEvent.position.y = ObjectManager.Instance.objectYToWorldSpace(newEvent.position.y);
        newEvent.endX = position.x + UnityEngine.Random.Range(-scoreIndicatorXRandomness, scoreIndicatorXRandomness);

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


    private Color GetIndicatorColor(ScoringEvent scoringEvent)
    {
        NoteEventType eventType = scoringEvent.noteEventType;
        if(eventType == NoteEventType.bad || eventType == NoteEventType.miss || eventType == NoteEventType.bomb)
        {
            return badColor;
        }
        else return Color.white;
    }


    private string GetIndicatorText(ScoringEvent scoringEvent)
    {
        switch(scoringEvent.noteEventType)
        {
            case NoteEventType.bad:
            case NoteEventType.bomb:
                return badCutString;
            case NoteEventType.miss:
                return missString;
            case NoteEventType.good:
            default:
                return scoringEvent.ScoreGained.ToString();
        }
    }


    private void UpdateScoreIndicator(ScoringEvent scoringEvent)
    {
        float timeDifference = TimeManager.CurrentTime - scoringEvent.Time;
        float t = timeDifference / indicatorLifetime;

        NoteEventType eventType = scoringEvent.noteEventType;
        bool isBad = eventType == NoteEventType.bad || eventType == NoteEventType.miss || eventType == NoteEventType.bomb;

        float endY = isBad ? badEndY : indicatorEndY;
        float endZ = isBad ? badEndZ : indicatorEndZ;

        Vector3 startPos = new Vector3(scoringEvent.position.x, scoringEvent.position.y, indicatorStartZ);
        Vector3 endPos = new Vector3(scoringEvent.endX, endY, endZ);
        Vector3 position = Vector3.Lerp(startPos, endPos, Easings.Quart.Out(t));

        Color color = GetIndicatorColor(scoringEvent);
        if(timeDifference < indicatorFadeInTime)
        {
            color.a = timeDifference / indicatorFadeInTime;
        }
        else
        {
            float endTime = scoringEvent.Time + indicatorLifetime;
            float fadeStartTime = endTime - indicatorFadeOutTime;
            if(TimeManager.CurrentTime >= fadeStartTime)
            {
                timeDifference = endTime - TimeManager.CurrentTime;
                color.a = timeDifference / indicatorFadeOutTime;
            }
        }

        if(scoringEvent.visual == null)
        {
            scoringEvent.visual = scoreIndicatorPool.GetObject();
            scoringEvent.visual.transform.SetParent(scoreIndicatorParent);
            scoringEvent.visual.gameObject.SetActive(true);

            RenderedScoringEvents.Add(scoringEvent);
        }

        scoringEvent.visual.transform.position = position;
        scoringEvent.visual.text = GetIndicatorText(scoringEvent);
        scoringEvent.visual.color = color;
    }


    private void ReleaseIndicator(ScoringEvent target)
    {
        scoreIndicatorPool.ReleaseObject(target.visual);
        target.visual = null;
        RenderedScoringEvents.Remove(target);
    }


    private void ClearOutsideIndicators()
    {
        for(int i = RenderedScoringEvents.Count - 1; i >= 0; i--)
        {
            ScoringEvent currentEvent = RenderedScoringEvents[i];
            if(currentEvent.Time > TimeManager.CurrentTime || currentEvent.Time + indicatorLifetime < TimeManager.CurrentTime)
            {
                ReleaseIndicator(currentEvent);
            }
        }
    }


    private void UpdateScoreIndicators(int lastEventIndex)
    {
        for(int i = lastEventIndex; i >= 0; i--)
        {
            ScoringEvent currentEvent = ScoringEvents[i];
            if(currentEvent.Time + indicatorLifetime >= TimeManager.CurrentTime)
            {
                UpdateScoreIndicator(currentEvent);
            }
            else break;
        }
    }


    private void UpdateBeat(float beat)
    {
        ClearOutsideIndicators();
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

            UpdateScoreIndicators(lastIndex);
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

        multiplierText.text = multiplierPrefix + ComboMultipliers[currentComboMult].ToString();
        comboProgressFill.fillAmount = (float)currentComboProgress / HitsNeededForComboIncrease[currentComboMult];
    }


    private void Reset()
    {
        ScoringEvents.Clear();
        hudObject.SetActive(false);

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
            hudObject.SetActive(true);

            TimeManager.OnBeatChanged += UpdateBeat;
            UpdateBeat(TimeManager.CurrentBeat);
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

    public Vector2 position;
    public float endX;

    public int Combo;
    public int ComboMult;
    public byte ComboProgress;

    public int Misses;

    public TextMeshPro visual;
}