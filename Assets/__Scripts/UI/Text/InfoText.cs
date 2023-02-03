using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class InfoText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI authorText;
    [SerializeField] private TextMeshProUGUI songText;
    [SerializeField] private TextMeshProUGUI mapperText;

    private BeatmapInfo info;


    public void UpdateText(BeatmapInfo newInfo)
    {
        info = newInfo;

        authorText.text = info._songAuthorName;
        songText.text = $"{info._songName} <i><size=70%>{info._songSubName}";
        mapperText.text = $"[{info._levelAuthorName}]";
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        //Display full text when the panel is moused over
        authorText.overflowMode = TextOverflowModes.Overflow;
        songText.overflowMode = TextOverflowModes.Overflow;
        mapperText.overflowMode = TextOverflowModes.Overflow;
    }

    
    public void OnPointerExit(PointerEventData eventData)
    {
        //Truncate the text when not moused over
        authorText.overflowMode = TextOverflowModes.Ellipsis;
        songText.overflowMode = TextOverflowModes.Ellipsis;
        mapperText.overflowMode = TextOverflowModes.Ellipsis;
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