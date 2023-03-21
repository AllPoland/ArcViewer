using UnityEngine;

public class ObjectSettingsUpdater : MonoBehaviour
{
    [SerializeField] private NoteManager noteManager;
    [SerializeField] private ChainManager chainManager;
    [SerializeField] private ArcManager arcManager;
    [SerializeField] private WallManager wallManager;


    public void UpdateObjectSettings()
    {
        noteManager.ClearRenderedNotes();
        noteManager.UpdateNoteVisuals(TimeManager.CurrentBeat);

        chainManager.ClearRenderedLinks();
        chainManager.UpdateChainVisuals(TimeManager.CurrentBeat);

        arcManager.UpdateMaterials();
        wallManager.UpdateMaterial();
    }


    private void Start()
    {
        SettingsManager.OnSettingsUpdated += UpdateObjectSettings;
        UpdateObjectSettings();
    }
}