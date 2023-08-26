using System.Collections.Generic;
using UnityEngine;

public class MistakeIconHandler : MonoBehaviour
{
    [SerializeField] private MistakeIcon iconPrefab;
    [SerializeField] private RectTransform iconParent;

    [Space]
    [SerializeField] private Sprite badcutSprite;
    [SerializeField] private Sprite missSprite;
    [SerializeField] private Sprite bombSprite;
    [SerializeField] private Sprite wallSprite;
    [SerializeField] private Sprite pauseSprite;

    [Space]
    [SerializeField] private Color badcutColor = Color.red;
    [SerializeField] private Color missColor = Color.white;
    [SerializeField] private Color bombColor = Color.yellow;
    [SerializeField] private Color wallColor = Color.magenta;
    [SerializeField] private Color pauseColor = Color.cyan;

    [Space]
    [SerializeField] private string badcutTooltip;
    [SerializeField] private string missTooltip;
    [SerializeField] private string bombTooltip;
    [SerializeField] private string wallTooltip;

    private List<MistakeIcon> icons = new List<MistakeIcon>();
    private Canvas parentCanvas;


    private string GetTimeString(float time)
    {
        int totalSeconds = Mathf.FloorToInt(time);
        int seconds = totalSeconds % 60;

        string secondsString = seconds >= 10 ? $"{seconds}" : $"0{seconds}";
        return $"{totalSeconds / 60}:{secondsString}";
    }


    private void SetIconProperties(ref MistakeIcon icon, ScoringEvent scoringEvent)
    {
        string timeString = GetTimeString(scoringEvent.ObjectTime);

        icon.SetParentReferences(iconParent, parentCanvas);
        icon.SetTime(scoringEvent.ObjectTime);

        if(scoringEvent.IsWall)
        {
            icon.SetVisual(wallSprite, wallColor);
            icon.SetTooltip(wallTooltip + timeString);
            return;
        }

        switch(scoringEvent.noteEventType)
        {
            default:
            case NoteEventType.bad:
                icon.SetVisual(badcutSprite, badcutColor);
                icon.SetTooltip(badcutTooltip + timeString);
                return;
            case NoteEventType.miss:
                icon.SetVisual(missSprite, missColor);
                icon.SetTooltip(missTooltip + timeString);
                return;
            case NoteEventType.bomb:
                icon.SetVisual(bombSprite, bombColor);
                icon.SetTooltip(bombTooltip + timeString);
                return;
        }
    }


    private void SetPauseIconProperties(ref MistakeIcon icon, Pause pauseEvent)
    {
        string timeString = GetTimeString(pauseEvent.time);
        string durationString = $"{Mathf.RoundToInt(pauseEvent.duration)}s";

        icon.SetParentReferences(iconParent, parentCanvas);
        icon.SetTime(pauseEvent.time);

        icon.SetVisual(pauseSprite, pauseColor);
        icon.SetTooltip($"Pause for {durationString} at {timeString}");
    }


    private void GenerateIcons()
    {
        ClearIcons();

        if(!ReplayManager.IsReplayMode)
        {
            return;
        }

        MapElementList<ScoringEvent> scoringEvents = ScoreManager.ScoringEvents;

        foreach(ScoringEvent scoringEvent in scoringEvents)
        {
            if(!scoringEvent.IsWall && !scoringEvent.IsBadHit)
            {
                continue;
            }

            MistakeIcon newIcon = Instantiate(iconPrefab, iconParent, false);

            SetIconProperties(ref newIcon, scoringEvent);
            icons.Add(newIcon);
        }

        foreach(Pause pauseEvent in ReplayManager.CurrentReplay.pauses)
        {
            MistakeIcon newIcon = Instantiate(iconPrefab, iconParent, false);

            SetPauseIconProperties(ref newIcon, pauseEvent);
            icons.Add(newIcon);
        }
    }


    private void ClearIcons()
    {
        for(int i = icons.Count - 1; i >= 0; i--)
        {
            icons[i].gameObject.SetActive(false);
            Destroy(icons[i].gameObject);
            icons.Remove(icons[i]);
        }
    }


    private void UpdateReplayMode(bool replayMode)
    {
        GenerateIcons();
    }


    private void UpdateDifficulty(Difficulty newDifficulty) => GenerateIcons();


    private void OnEnable()
    {
        if(!parentCanvas)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }

        ReplayManager.OnReplayModeChanged += UpdateReplayMode;
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateDifficulty;

        GenerateIcons();
    }


    private void OnDisable()
    {
        ClearIcons();
    }
}