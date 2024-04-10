using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class InfoPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI authorText;
    [SerializeField] private TextMeshProUGUI songText;
    [SerializeField] private TextMeshProUGUI mapperText;

    private BeatmapInfo info;


    public void UpdateText(Difficulty newDifficulty)
    {
        info = BeatmapManager.Info;

        authorText.text = info.song.author;
        songText.text = $"{info.song.title} <i><size=70%>{info.song.subTitle}";

        List<string> mappers = newDifficulty.mappers.ToList();
        mappers.AddRange(newDifficulty.lighters);

        if(mappers.Count > 0)
        {
            mapperText.text = $"[{string.Join(", ", mappers)}]";
        }
        else mapperText.text = "";
    }


    public void ToggleSharePanel()
    {
        DialogueHandler.Instance.SetSharePanelActive(!DialogueHandler.Instance.sharePanel.activeInHierarchy);
    }


    public void ToggleJumpSettingsPanel()
    {
        DialogueHandler.Instance.SetJumpSettingsPanelActive(!DialogueHandler.Instance.jumpSettingsPanel.activeInHierarchy);
        DialogueHandler.Instance.SetStatsPanelActive(false);
    }


    public void ToggleStatsPanel()
    {
        DialogueHandler.Instance.SetStatsPanelActive(!DialogueHandler.Instance.statsPanel.activeInHierarchy);
        DialogueHandler.Instance.SetJumpSettingsPanelActive(false);
    }


    private void OnEnable()
    {
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateText;

        UpdateText(BeatmapManager.CurrentDifficulty ?? Difficulty.Empty);
    }


    private void OnDisable()
    {
        BeatmapManager.OnBeatmapDifficultyChanged -= UpdateText;
    }
}