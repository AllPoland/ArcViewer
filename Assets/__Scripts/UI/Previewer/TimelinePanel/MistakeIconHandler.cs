using System.Collections.Generic;
using UnityEngine;

public class MistakeIconHandler : MonoBehaviour
{
    [SerializeField] private GameObject iconPrefab;
    [SerializeField] private RectTransform iconParent;

    [Space]
    [SerializeField] private Sprite badcutSprite;
    [SerializeField] private Sprite missSprite;
    [SerializeField] private Sprite bombSprite;
    [SerializeField] private Sprite wallSprite;

    [Space]
    [SerializeField] private Color badcutColor = Color.red;
    [SerializeField] private Color missColor = Color.white;
    [SerializeField] private Color bombColor = Color.yellow;
    [SerializeField] private Color wallColor = Color.magenta;

    [Space]
    [SerializeField] private string badcutTooltip;
    [SerializeField] private string missTooltip;
    [SerializeField] private string bombTooltip;
    [SerializeField] private string wallTooltip;

    private List<MistakeIcon> icons = new List<MistakeIcon>();
    private Canvas parentCanvas;


    private void SetIconProperties(ref MistakeIcon icon, ScoringEvent scoringEvent)
    {
        int timeSeconds = Mathf.RoundToInt(scoringEvent.ObjectTime);
        string timeString = $"{timeSeconds / 60}:{timeSeconds % 60}";

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


    private void GenerateIcons()
    {
        ClearIcons();

        MapElementList<ScoringEvent> scoringEvents = ScoreManager.ScoringEvents;

        foreach(ScoringEvent scoringEvent in scoringEvents)
        {
            if(!scoringEvent.IsWall && !scoringEvent.IsBadHit)
            {
                continue;
            }

            GameObject newIconObject = Instantiate(iconPrefab, iconParent, false);
            MistakeIcon newIcon = newIconObject.GetComponent<MistakeIcon>();

            if(!newIcon)
            {
                Debug.LogError("The mistake icon prefab doesn't include a MistakeIcon component!");
                return;
            }

            SetIconProperties(ref newIcon, scoringEvent);
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
        if(replayMode)
        {
            GenerateIcons();
        }
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

        if(ReplayManager.IsReplayMode)
        {
            GenerateIcons();
        }
    }


    private void OnDisable()
    {
        ClearIcons();
    }
}