using TMPro;
using UnityEngine;

public class CurrentStatsPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI bpmText;
    [SerializeField] private TextMeshProUGUI njsText;

    [Space]
    [SerializeField] private float previewModeY;
    [SerializeField] private float replayModeY;

    private bool active = false;


    private void UpdateCurrentBPM()
    {
        string bpm = TimeManager.CurrentBPM.Round(3).ToString();
        bpmText.text = $"BPM: {bpm}";
    }


    private void UpdateCurrentNJS()
    {
        string njs = ObjectManager.Instance.jumpManager.NJS.Round(3).ToString();
        njsText.text = $"NJS: {njs}";
    }


    private void UpdateSettings(string setting)
    {
        if(setting == "all" || setting == "showcurrentstats")
        {
            active = SettingsManager.GetBool("showcurrentstats");
            bpmText.gameObject.SetActive(active);
            njsText.gameObject.SetActive(active);
        }
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;
        if(SettingsManager.Loaded)
        {
            UpdateSettings("all");
        }
        
        //Adjust Y position to account for the player info panel being enabled/disabled
        RectTransform rectTransform = (RectTransform)transform;
        float yPos = ReplayManager.IsReplayMode ? replayModeY : previewModeY;
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, yPos);

        if(active)
        {
            UpdateCurrentBPM();
            UpdateCurrentNJS();
        }
    }


    private void LateUpdate()
    {
        if(active)
        {
            UpdateCurrentBPM();
            UpdateCurrentNJS();
        }
    }
}