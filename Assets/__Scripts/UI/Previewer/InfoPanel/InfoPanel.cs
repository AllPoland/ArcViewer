using System;
using UnityEngine;
using TMPro;

public class InfoPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI authorText;
    [SerializeField] private TextMeshProUGUI songText;
    [SerializeField] private TextMeshProUGUI mapperText;
    [SerializeField] private TextMeshProUGUI lighterText;

    [SerializeField] private GameObject mapperContainer;
    [SerializeField] private GameObject lighterContainer;

    private BeatmapInfo info;


    public void UpdateText(Difficulty newDifficulty)
    {
        info = BeatmapManager.Info;

        authorText.text = info.song.author;
        songText.text = $"{info.song.title} <i><size=70%>{info.song.subTitle}";

        string[] mappers = newDifficulty.mappers;
        string[] lighters = newDifficulty.lighters;

        if(mappers.Length == 1 && lighters.Length == 1 && mappers[0].Equals(lighters[0], StringComparison.InvariantCultureIgnoreCase))
        {
            //The same name is listed as mapper and lighter, just show the name once
            mapperContainer.SetActive(true);
            lighterContainer.SetActive(false);
            mapperText.text = mappers[0];
        }
        else
        {
            mapperContainer.SetActive(mappers.Length > 0);
            lighterContainer.SetActive(lighters.Length > 0);

            mapperText.text = string.Join(", ", mappers);
            lighterText.text = string.Join(", ", lighters);
        }
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