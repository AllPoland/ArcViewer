using UnityEngine;
using TMPro;

public class StatsPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI notesText;
    [SerializeField] private TextMeshProUGUI bombsText;
    [SerializeField] private TextMeshProUGUI wallsText;
    [SerializeField] private TextMeshProUGUI arcsText;
    [SerializeField] private TextMeshProUGUI chainsText;
    [SerializeField] private TextMeshProUGUI eventsText;
    [SerializeField] private TextMeshProUGUI bpmEventsText;
    [SerializeField] private TextMeshProUGUI currentBPMText;

    [SerializeField] private TextMeshProUGUI npsText;
    [SerializeField] private TextMeshProUGUI peakNps16Text;
    [SerializeField] private TextMeshProUGUI peakNps8Text;
    [SerializeField] private TextMeshProUGUI peakNps4Text;

    [SerializeField] private TextMeshProUGUI spsText;
    [SerializeField] private TextMeshProUGUI peakSps16Text;
    [SerializeField] private TextMeshProUGUI peakSps8Text;
    [SerializeField] private TextMeshProUGUI peakSps4Text;


    private void UpdateText()
    {
        if(!ObjectManager.Instance)
        {
            //Funky way to avoid trying to update stats on start when we'll just get nullrefs
            return;
        }

        notesText.text = $"Notes: {MapStats.NoteCount}";
        bombsText.text = $"Bombs: {MapStats.BombCount}";
        wallsText.text = $"Walls: {MapStats.WallCount}";
        arcsText.text = $"Arcs: {MapStats.ArcCount}";
        chainsText.text = $"Chains: {MapStats.ChainCount}";
        eventsText.text = $"Events: {MapStats.EventCount}";
        bpmEventsText.text = $"BPM Events: {MapStats.BpmEventCount}";

        npsText.text = $"Notes Per Second: {MapStats.NotesPerSecond}";
        peakNps16Text.text = $"16 Beat Peak: {MapStats.NpsPeak16}";
        peakNps8Text.text = $"8 Beat Peak: {MapStats.NpsPeak8}";
        peakNps4Text.text = $"4 Beat Peak: {MapStats.NpsPeak4}";

        spsText.text = $"Swings Per Second: {MapStats.SwingsPerSecond}";
        peakSps16Text.text = $"16 Beat Peak: {MapStats.SpsPeak16}";
        peakSps8Text.text = $"8 Beat Peak: {MapStats.SpsPeak8}";
        peakSps4Text.text = $"4 Beat Peak: {MapStats.SpsPeak4}";
    }


    private void UpdateCurrentBPM()
    {
        string bpm = TimeManager.CurrentBPM.ToString();
        currentBPMText.text = $"Current BPM: {bpm}";
    }


    private void UpdateBeat(float beat)
    {
        UpdateCurrentBPM();
    }


    private void OnEnable()
    {
        MapStats.OnStatsUpdated += UpdateText;
        TimeManager.OnBeatChanged += UpdateBeat;

        UpdateText();
        UpdateCurrentBPM();
    }


    private void OnDisable()
    {
        MapStats.OnStatsUpdated -= UpdateText;
        TimeManager.OnBeatChanged -= UpdateBeat;
    }
}