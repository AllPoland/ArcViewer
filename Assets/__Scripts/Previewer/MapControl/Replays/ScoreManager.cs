using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static MapElementList<ScoringEvent> ScoringEvents = new MapElementList<ScoringEvent>();
    private static List<ScoringEvent> RenderedScoringEvents = new List<ScoringEvent>();

    public static int MaxScore { get; private set; }
    public static int TotalScore => ScoringEvents.Count > 0 ? ScoringEvents.Last.TotalScore : 0;


    [Header("Components")]
    [SerializeField] private GameObject hudObject;

    [Space]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI scorePercentageText;
    [SerializeField] private TextMeshProUGUI fcPercentageText;
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI missText;

    [Space]
    [SerializeField] private TextMeshProUGUI multiplierText;
    [SerializeField] private Image comboProgressFill;
    [SerializeField] private RectTransform FCBars;

    [Space]
    [SerializeField] private TMProPool scoreIndicatorPool;
    [SerializeField] private Transform scoreIndicatorParent;

    [Space]
    [SerializeField] private RectTransform energyBar;
    [SerializeField] private RectTransform energyBarFill;

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
    [SerializeField] private string missString;

    [Header("Colors")]
    [SerializeField] private Color badColor = Color.white;
    [SerializeField] private ScoreColorSettings[] colorSettings;

    private ScoreColorSettings currentColorSettings = new ScoreColorSettings();

    public static bool IsScoreSaberLegacyReplay => ReplayManager.IsReplayMode
        && ReplayManager.CurrentReplay?.scoreSaberLegacyScoreData != null;


    public static void InitializeMapScore()
    {
        int currentScore = 0;
        int fcScore = 0;
        int maxScore = 0;
        int maxScoreNoMisses = 0;

        int combo = 0;
        byte comboMult = 0;
        byte comboProgress = 0;

        byte maxComboMult = 0;
        byte maxComboProgress = 0;

        byte fcComboMult = 0;
        byte fcComboProgress = 0;

        int misses = 0;

        int inferCount = 0;
        for(int i = 0; i < ScoringEvents.Count; i++)
        {
            ScoringEvent currentEvent = ScoringEvents[i];

            if(!currentEvent.Initialized)
            {
                if(currentEvent.IsWall)
                {
                    //Wall hits need to get their position from the player head
                    Vector3 headPosition = PlayerPositionManager.HeadPositionAtTime(currentEvent.Time);
                    currentEvent.SetEventValues(ScoringType.NoScore, headPosition);
                }
                else
                {
                    //This event wasn't matched with a note, so its type and position need to be inferred
                    currentEvent.InferEventValues();
                    inferCount++;
                }
            }

            if(currentEvent.scoringType == ScoringType.Ignore)
            {
                continue;
            }

            if(maxComboMult < ScoringUtils.ComboMultipliers.Length - 1 && currentEvent.scoringType != ScoringType.NoScore)
            {
                //Increment max combo, which is used to calculate max score
                maxComboProgress++;
                if(maxComboProgress >= ScoringUtils.HitsNeededForComboIncrease[maxComboMult])
                {
                    maxComboMult++;
                    maxComboProgress = 0;
                }
            }

            int maxEventScore;
            switch(currentEvent.scoringType)
            {
                default:
                case ScoringType.Note:
                case ScoringType.ArcHead:
                case ScoringType.ArcTail:
                case ScoringType.ArcHeadArcTail:
                    maxEventScore = ScoringUtils.MaxNoteScore;
                    break;
                case ScoringType.ChainHead:
                case ScoringType.ChainHeadArcHead:
                case ScoringType.ChainHeadArcTail:
                case ScoringType.ChainHeadArcHeadArcTail:
                    maxEventScore = ScoringUtils.MaxChainHeadScore;
                    break;
                case ScoringType.ChainLink:
                case ScoringType.ChainLinkArcHead:
                    maxEventScore = ScoringUtils.MaxChainLinkScore;
                    break;
            }
            maxScore += maxEventScore * ScoringUtils.ComboMultipliers[maxComboMult];

            if(currentEvent.IsBadHit)
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
                if(comboMult < ScoringUtils.ComboMultipliers.Length - 1)
                {
                    //Combo is below max, and should be incremented
                    comboProgress++;
                    if(comboProgress >= ScoringUtils.HitsNeededForComboIncrease[comboMult])
                    {
                        //Combo multiplier has increased
                        comboMult++;
                        comboProgress = 0;
                    }
                }

                currentScore += currentEvent.ScoreGained * ScoringUtils.ComboMultipliers[comboMult];

                //Count this hit towards the FC score
                if(fcComboMult < ScoringUtils.ComboMultipliers.Length - 1 && currentEvent.scoringType != ScoringType.NoScore)
                {
                    //Increment FC combo, which is used to calculate FC percentage
                    fcComboProgress++;
                    if(fcComboProgress >= ScoringUtils.HitsNeededForComboIncrease[fcComboMult])
                    {
                        fcComboMult++;
                        fcComboProgress = 0;
                    }
                }

                fcScore += currentEvent.ScoreGained * ScoringUtils.ComboMultipliers[fcComboMult];
                maxScoreNoMisses += maxEventScore * ScoringUtils.ComboMultipliers[fcComboMult];
            }

            currentEvent.TotalScore = currentScore;
            currentEvent.FCScore = fcScore;
            currentEvent.MaxScore = maxScore;
            currentEvent.MaxScoreNoMisses = maxScoreNoMisses;
            currentEvent.Combo = combo;
            currentEvent.ComboMult = comboMult;
            currentEvent.ComboProgress = comboProgress;
            currentEvent.Misses = misses;

            currentEvent.ScorePercentage = maxScore == 0 ? 100f : ((float)currentScore / maxScore) * 100;
            currentEvent.FCScorePercentage = maxScoreNoMisses == 0 ? 100f : ((float)fcScore / maxScoreNoMisses) * 100;
            // Debug.Log($"Event #{i} | Time: {Math.Round(currentEvent.Time, 2)} | Type: {currentEvent.scoringType} | Score: {currentEvent.ScoreGained} | Total score: {currentScore} | Max score: {maxScore} | Combo: {combo} | Combo mult: {ScoringUtils.ComboMultipliers[comboMult]}x");
        }

        if(inferCount > 0)
        {
            Debug.LogWarning($"Unable to match {inferCount} scoring events to their notes!");
            ErrorHandler.Instance.ShowPopup(ErrorType.Warning, $"Couldn't match {inferCount} replay notes to the map!");
        }

        if(!IsScoreSaberLegacyReplay && currentScore != ReplayManager.CurrentReplay.info.score)
        {
            Debug.LogWarning($"Calculated score does not match the metadata score: {ReplayManager.CurrentReplay.info.score}!");
        }

        Debug.Log($"Initialized Scoring Events for replay with {ScoringEvents.Count} events. Total score: {TotalScore} out of max: {maxScore} with {misses} misses.");

        //Energy needs to be calculated per-frame because of walls
        //I know it's super jank and spaghetti to have that happen in a class called
        //"PlayerPositionManager" but I don't care
        if(!IsScoreSaberLegacyReplay)
        {
            PlayerPositionManager.InitializeEnergyValues(ScoringEvents);
        }
    }


    public void UpdateObjects()
    {
        Replay replay = ReplayManager.CurrentReplay;
        if(ScoreSaberLegacyConverter.NeedsConversion(replay))
        {
            ScoreSaberLegacyConverter.Convert(replay, BeatmapManager.CurrentDifficulty.beatmapDifficulty);

            // rebuild scoring events from the synthetic notes
            ScoringEvents.Clear();
            foreach(NoteEvent noteEvent in replay.notes)
            {
                ScoringEvent newEvent = new ScoringEvent(noteEvent);
                if(ScoringEvents.Last != null && noteEvent.eventTime < ScoringEvents.Last.Time)
                {
                    ScoringEvents.InsertSorted(newEvent);
                }
                else ScoringEvents.Add(newEvent);
            }

            // retrigger ObjectManager to process notes with the new events
            ObjectManager.Instance.UpdateDifficulty(BeatmapManager.CurrentDifficulty);
            return;
        }

        InitializeMapScore();
        UpdateBeat(TimeManager.CurrentBeat);
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


    private static string FormatScore(int score)
    {
        string digits = score.ToString();
        string result = "";
        int end = digits.Length;
        for(int i = end - 1; i >= 0; i -= 3)
        {
            int start = Mathf.Max(i - 2, 0);
            string chunk = digits.Substring(start, end - start);
            result = result.Length == 0 ? chunk : chunk + ' ' + result;
            end = start;
        }
        return result;
    }


    private static string GetPercentageString(float percentage)
    {
        float roundedPercentage = (float)Math.Round(percentage, 2);
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

        return percentageString;
    }


    private ScoreTextInfo GetIndicatorInfo(ScoringEvent scoringEvent)
    {
        if(scoringEvent.IsBadHit)
        {
            return new ScoreTextInfo(0, badColor);
        }
        else if(scoringEvent.scoringType == ScoringType.ChainLink
            || scoringEvent.scoringType == ScoringType.ChainLinkArcHead)
        {
            return new ScoreTextInfo(ScoringUtils.MaxChainLinkScore, currentColorSettings.chainLinkColor);
        }
        else
        {
            int scoreGained = scoringEvent.ScoreGained;
            if(scoringEvent.scoringType == ScoringType.ChainHead
                || scoringEvent.scoringType == ScoringType.ChainHeadArcHead
                || scoringEvent.scoringType == ScoringType.ChainHeadArcTail
                || scoringEvent.scoringType == ScoringType.ChainHeadArcHeadArcTail)
            {
                //Adjust for the missing post swing points on chain heads
                scoreGained += ScoringUtils.PostSwingValue;
            }
            return currentColorSettings.GetScoreTextInfo(scoringEvent);
        }
    }


    private void UpdateScoreIndicator(ScoringEvent scoringEvent)
    {
        if(scoringEvent.visual == null)
        {
            scoringEvent.visual = scoreIndicatorPool.GetObject();
            scoringEvent.visual.transform.SetParent(scoreIndicatorParent);
            scoringEvent.visual.gameObject.SetActive(true);

            RenderedScoringEvents.Add(scoringEvent);

            //Get the score text and color based on HSV config
            scoringEvent.textInfo = GetIndicatorInfo(scoringEvent);
            if(scoringEvent.noteEventType == NoteEventType.bad || scoringEvent.noteEventType == NoteEventType.bomb)
            {
                //Use the X icon for this indicator
                scoringEvent.visual.SetIconActive(true);
            }
            else
            {
                //If the note was missed, use the miss text
                //Otherwise, use the formatted string from the config
                bool isMiss = scoringEvent.noteEventType == NoteEventType.miss;
                string indicatorText = isMiss ? missString : scoringEvent.textInfo.text;

                scoringEvent.visual.SetIconActive(false);
                scoringEvent.visual.SetText(indicatorText);
            }
        }

        float timeDifference = TimeManager.CurrentTime - scoringEvent.Time;
        float t = timeDifference / indicatorLifetime;

        NoteEventType eventType = scoringEvent.noteEventType;
        bool isBad = eventType == NoteEventType.bad || eventType == NoteEventType.miss || eventType == NoteEventType.bomb;

        float endY = isBad ? badEndY : indicatorEndY;
        float endZ = isBad ? badEndZ : indicatorEndZ;

        Vector3 startPos = new Vector3(scoringEvent.position.x, scoringEvent.position.y, indicatorStartZ);
        Vector3 endPos = new Vector3(scoringEvent.endX, endY, endZ);
        Vector3 position = Vector3.Lerp(startPos, endPos, Easings.Quart.Out(t));

        Color color = scoringEvent.textInfo.color;
        if(timeDifference < indicatorFadeInTime)
        {
            color.a *= timeDifference / indicatorFadeInTime;
        }
        else
        {
            float endTime = scoringEvent.Time + indicatorLifetime;
            float fadeStartTime = endTime - indicatorFadeOutTime;
            if(TimeManager.CurrentTime >= fadeStartTime)
            {
                timeDifference = endTime - TimeManager.CurrentTime;
                color.a *= timeDifference / indicatorFadeOutTime;
            }
        }

        scoringEvent.visual.SetColor(color);
        scoringEvent.visual.transform.position = position;
    }


    private void ReleaseIndicator(ScoringEvent target)
    {
        scoreIndicatorPool.ReleaseObject(target.visual);
        target.visual = null;
        RenderedScoringEvents.Remove(target);
    }


    private void ClearIndicators()
    {
        for(int i = RenderedScoringEvents.Count - 1; i >= 0; i--)
        {
            ReleaseIndicator(RenderedScoringEvents[i]);
        }
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
        if(IsScoreSaberLegacyReplay)
        {
            UpdateBeatScoreSaberLegacy();
            return;
        }

        ClearOutsideIndicators();
        int lastIndex = ScoringEvents.GetLastIndex(TimeManager.CurrentTime, x => x.Time <= TimeManager.CurrentTime);

        int currentScore;
        float currentPercentage;
        float currentFCPercentage;
        int currentCombo;
        int currentComboMult;
        int currentComboProgress;
        int currentMisses;
        if(lastIndex >= 0)
        {
            ScoringEvent lastEvent = ScoringEvents[lastIndex];

            currentScore = lastEvent.TotalScore;
            currentPercentage = lastEvent.ScorePercentage;
            currentFCPercentage = lastEvent.FCScorePercentage;
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
            currentFCPercentage = 100f;
            currentCombo = 0;
            currentComboMult = 0;
            currentComboProgress = 0;
            currentMisses = 0;
        }

        comboText.text = currentCombo.ToString();
        missText.text = currentMisses.ToString();

        float effectivePercentage = currentPercentage * ReplayManager.ModifierMult;
        if(ReplayManager.HasFailed)
        {
            effectivePercentage *= 0.5f;
        }
        gradeText.text = GradeFromPercentage(effectivePercentage);

        scoreText.text = FormatScore(currentScore);

        scorePercentageText.text = GetPercentageString(currentPercentage);
        fcPercentageText.text = $"FC : {GetPercentageString(currentFCPercentage)}";

        multiplierText.text = multiplierPrefix + ScoringUtils.ComboMultipliers[currentComboMult].ToString();
        comboProgressFill.fillAmount = (float)currentComboProgress / ScoringUtils.HitsNeededForComboIncrease[currentComboMult];
        FCBars.gameObject.SetActive(currentMisses <= 0);

        float healthBarWidth = energyBar.sizeDelta.x;
        energyBarFill.sizeDelta = new Vector2(healthBarWidth * PlayerPositionManager.Energy, energyBarFill.sizeDelta.y);
    }


    private void UpdateBeatScoreSaberLegacy()
    {
        Replay replay = ReplayManager.CurrentReplay;
        List<LegacyScoreFrame> legacyData = replay.scoreSaberLegacyScoreData;
        if(legacyData == null || legacyData.Count == 0) return;

        // update flying score indicators from synthetic events
        ClearOutsideIndicators();
        int lastEventIndex = ScoringEvents.GetLastIndex(TimeManager.CurrentTime, x => x.Time <= TimeManager.CurrentTime);
        if(lastEventIndex >= 0)
        {
            UpdateScoreIndicators(lastEventIndex);
        }

        // find the last legacy frame at or before current time
        int legacyScore = 0;
        int legacyCombo = 0;
        for(int i = 0; i < legacyData.Count; i++)
        {
            if(legacyData[i].time > TimeManager.CurrentTime) break;

            legacyScore = legacyData[i].score;
            legacyCombo = legacyData[i].combo;
        }

        int misses = lastEventIndex >= 0 ? ScoringEvents[lastEventIndex].Misses : 0;

        byte comboMult = 0;
        byte comboProgress = 0;
        for(int c = 0; c < legacyCombo; c++)
            ScoringUtils.ManuallyAdvanceCombo(ref comboMult, ref comboProgress);

        // find max score at current time from precomputed HUD data
        int maxScore = 0;
        List<LegacyHUDData.MaxScoreFrame> maxScores = replay.scoreSaberLegacyHUDData?.maxScores;
        if(maxScores != null)
        {
            for(int i = maxScores.Count - 1; i >= 0; i--)
            {
                if(maxScores[i].time <= TimeManager.CurrentTime)
                {
                    maxScore = maxScores[i].maxScore;
                    break;
                }
            }
        }

        float percentage = maxScore > 0 ? Mathf.Min(((float)legacyScore / maxScore) * 100, 100f) : 100f;

        scoreText.text = FormatScore(legacyScore);
        comboText.text = legacyCombo.ToString();
        missText.text = misses.ToString();

        scorePercentageText.text = GetPercentageString(percentage);
        gradeText.text = GradeFromPercentage(percentage);

        multiplierText.text = multiplierPrefix + ScoringUtils.ComboMultipliers[comboMult].ToString();
        comboProgressFill.fillAmount = (float)comboProgress / ScoringUtils.HitsNeededForComboIncrease[comboMult];
        FCBars.gameObject.SetActive(misses <= 0);

        // hide elements we can't calculate for legacy replays
        fcPercentageText.gameObject.SetActive(false);
        energyBar.gameObject.SetActive(false);
    }


    private void Reset()
    {
        ClearIndicators();
        ScoringEvents.Clear();
        hudObject.SetActive(false);

        missText.gameObject.SetActive(true);
        energyBar.gameObject.SetActive(true);
        fcPercentageText.gameObject.SetActive(SettingsManager.GetBool("fcacc"));

        TimeManager.OnBeatChanged -= UpdateBeat;
        ObjectManager.OnObjectsLoaded -= UpdateObjects;
    }


    private void UpdateReplayMode(bool replayMode)
    {
        if(replayMode)
        {
            bool showHud = SettingsManager.GetBool("showhud");
            ScoringEvents.Clear();
            hudObject.SetActive(showHud);

            if(IsScoreSaberLegacyReplay)
            {
                ErrorHandler.Instance.ShowPopup(ErrorType.Warning, "This is a legacy ScoreSaber replay. Some data is synthesised and may not be fully accurate.");
            }

            foreach(NoteEvent noteEvent in ReplayManager.CurrentReplay.notes)
            {
                ScoringEvent newEvent = new ScoringEvent(noteEvent);
                if(ScoringEvents.Last != null && noteEvent.eventTime < ScoringEvents.Last.Time)
                {
                    //Events need to be kept in order individually, full list sorting
                    //should be avoided
                    ScoringEvents.InsertSorted(newEvent);
                }
                else ScoringEvents.Add(newEvent);
            }

            foreach(WallEvent wallEvent in ReplayManager.CurrentReplay.walls)
            {
                ScoringEvents.InsertSorted(new ScoringEvent(wallEvent));
            }

            ObjectManager.OnObjectsLoaded += UpdateObjects;
            if(showHud)
            {
                TimeManager.OnBeatChanged += UpdateBeat;
                UpdateBeat(TimeManager.CurrentBeat);
            }
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


    private void UpdateScoreColorSettings()
    {
        if(SettingsManager.GetBool("customhsvconfig") && HsvLoader.CustomHSV != null)
        {
            currentColorSettings = HsvLoader.CustomHSV;
        }
        else
        {
            int colorSettingsIndex = SettingsManager.GetInt("scorecolortype");
            colorSettingsIndex = Mathf.Clamp(colorSettingsIndex, 0, colorSettings.Length - 1);
            currentColorSettings = colorSettings[colorSettingsIndex];
        }

        if(ReplayManager.IsReplayMode && SettingsManager.GetBool("showhud"))
        {
            ClearIndicators();
            UpdateBeat(TimeManager.CurrentBeat);
        }
    }


    private void UpdateCustomHSV()
    {
        if(SettingsManager.Loaded && SettingsManager.GetBool("customhsvconfig"))
        {
            UpdateScoreColorSettings();
        }
    }


    private void UpdateSettings(string setting)
    {
        bool allSettings = setting == "all";
        if(allSettings || setting == "showhud")
        {
            bool showHud = SettingsManager.GetBool("showhud");
            bool shouldEnable = ReplayManager.IsReplayMode && showHud;

            hudObject.gameObject.SetActive(shouldEnable);
            if(shouldEnable)
            {
                TimeManager.OnBeatChanged += UpdateBeat;
                UpdateBeat(TimeManager.CurrentBeat);
            }
            else
            {
                TimeManager.OnBeatChanged -= UpdateBeat;
                ClearIndicators();
            }
        }

        if(colorSettings.Length > 0 && (allSettings || setting == "scorecolortype" || setting == "customhsvconfig"))
        {
            UpdateScoreColorSettings();
        }
        
        if(allSettings || setting == "fcacc")
        {
            bool showFCPercentage = SettingsManager.GetBool("fcacc");
            fcPercentageText.gameObject.SetActive(showFCPercentage);
        }
    }


    private void Start()
    {
        ReplayManager.OnReplayModeChanged += UpdateReplayMode;
        UIStateManager.OnUIStateChanged += UpdateUIState;
        SettingsManager.OnSettingsUpdated += UpdateSettings;
        HsvLoader.OnCustomHSVUpdated += UpdateCustomHSV;

        if(SettingsManager.Loaded)
        {
            UpdateSettings("all");
        }
    }
}