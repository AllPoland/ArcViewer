using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InfoPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI authorText;
    [SerializeField] private TextMeshProUGUI songText;
    [SerializeField] private TextMeshProUGUI mapperText;
    [SerializeField] private TextMeshProUGUI lighterText;

    [SerializeField] private GameObject mapperContainer;
    [SerializeField] private GameObject lighterContainer;
    [SerializeField] private Image mapperIcon;
    [SerializeField] private Image lighterIcon;

    private BeatmapInfo info;


    public void UpdateText(Difficulty newDifficulty)
    {
        info = BeatmapManager.Info;

        authorText.text = info.song.author;
        songText.text = $"{info.song.title} <i><size=70%>{info.song.subTitle}";

        List<string> mappers = newDifficulty.mappers?.ToList() ?? new List<string>();
        List<string> lighters = newDifficulty.lighters?.ToList() ?? new List<string>();

        //Remove empty strings from the mapper fields
        for(int i = mappers.Count - 1; i >= 0; i--)
        {
            if(string.IsNullOrEmpty(mappers[i]))
            {
                mappers.RemoveAt(i);
            }
        }
        for(int i = lighters.Count - 1; i >= 0; i--)
        {
            if(string.IsNullOrEmpty(lighters[i]))
            {
                lighters.RemoveAt(i);
            }
        }

        //Check if both lists contain the same mappers/lighters
        bool equalContributors = mappers.Count == lighters.Count;
        if(equalContributors)
        {
            for(int i = 0; i < mappers.Count; i++)
            {
                if(mappers[i] != lighters[i])
                {
                    //The names in mappers/lighters don't match
                    equalContributors = false;
                    break;
                }
            }
        }

        if(equalContributors)
        {
            //The same names are listed as mapper and lighter, just show one field
            mapperContainer.SetActive(mappers.Count > 0);
            lighterContainer.SetActive(false);

            mapperText.text = mapperText.text = string.Join(", ", mappers);
        }
        else
        {
            mapperContainer.SetActive(mappers.Count > 0);
            lighterContainer.SetActive(lighters.Count > 0);

            mapperText.text = string.Join(", ", mappers);
            lighterText.text = string.Join(", ", lighters);
        }

        bool useIcons = mapperContainer.activeInHierarchy && lighterContainer.activeInHierarchy;
        mapperIcon.gameObject.SetActive(useIcons);
        lighterIcon.gameObject.SetActive(useIcons);
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