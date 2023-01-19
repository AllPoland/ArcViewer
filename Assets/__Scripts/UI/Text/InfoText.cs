using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InfoText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI songText;
    [SerializeField] private TextMeshProUGUI mapperText;


    public void UpdateText(BeatmapInfo info)
    {
        songText.text = $"{info._songAuthorName} - {info._songName}";
        mapperText.text = $"[{info._levelAuthorName}]";
    }


    private void OnEnable()
    {
        BeatmapManager.OnBeatmapInfoChanged += UpdateText;

        UpdateText(BeatmapManager.Info ?? new BeatmapInfo());
    }


    private void OnDisable()
    {
        BeatmapManager.OnBeatmapInfoChanged -= UpdateText;
    }
}