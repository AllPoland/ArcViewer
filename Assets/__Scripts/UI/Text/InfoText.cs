using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InfoText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI songText;
    [SerializeField] private TextMeshProUGUI mapperText;

    private BeatmapManager beatmapManager;


    public void UpdateText(BeatmapInfo info)
    {
        songText.text = $"{info._songAuthorName} - {info._songName}";
        mapperText.text = $"[{info._levelAuthorName}]";
    }


    private void OnEnable()
    {
        if(beatmapManager == null) beatmapManager = BeatmapManager.Instance;

        if(beatmapManager != null)
        {
            beatmapManager.OnBeatmapInfoChanged += UpdateText;
        }

        UpdateText(beatmapManager?.Info ?? new BeatmapInfo());
    }


    private void OnDisable()
    {
        if(beatmapManager != null)
        {
            beatmapManager.OnBeatmapInfoChanged -= UpdateText;
        }
    }
}