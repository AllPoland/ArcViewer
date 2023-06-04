using UnityEngine;
using TMPro;

public class StatsPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI notesText;
    [SerializeField] private TextMeshProUGUI bombsText;
    [SerializeField] private TextMeshProUGUI wallsText;
    [SerializeField] private TextMeshProUGUI arcsText;
    [SerializeField] private TextMeshProUGUI chainsText;

    private NoteManager noteManager => ObjectManager.Instance.noteManager;
    private BombManager bombManager => ObjectManager.Instance.bombManager;
    private WallManager wallManager => ObjectManager.Instance.wallManager;
    private ArcManager arcManager => ObjectManager.Instance.arcManager;
    private ChainManager chainManager => ObjectManager.Instance.chainManager;


    private void UpdateStats()
    {
        if(!ObjectManager.Instance)
        {
            return;
        }

        notesText.text = $"Notes: {noteManager.Objects.Count}";
        bombsText.text = $"Bombs: {bombManager.Objects.Count}";
        wallsText.text = $"Walls: {wallManager.Objects.Count}";
        arcsText.text = $"Arcs: {arcManager.Objects.Count}";
        chainsText.text = $"Chains: {chainManager.Objects.Count}";
    }


    private void OnEnable()
    {
        UpdateStats();
    }
}