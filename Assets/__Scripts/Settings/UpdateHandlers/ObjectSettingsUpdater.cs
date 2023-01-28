using UnityEngine;

public class ObjectSettingsUpdater : MonoBehaviour
{
    [SerializeField] private NoteManager noteManager;
    [SerializeField] private ChainManager chainManager;


    public void UpdateObjectSettings()
    {
        bool simpleNotes = SettingsManager.GetBool("simplenotes");
        bool moveAnimations = SettingsManager.GetBool("moveanimations");
        bool rotateAnimations = SettingsManager.GetBool("rotateanimations");

        noteManager.useSimpleNoteMaterial = simpleNotes;
        noteManager.doMovementAnimation = moveAnimations;
        noteManager.doRotationAnimation = rotateAnimations;

        chainManager.useSimpleNoteMaterial = simpleNotes;
        chainManager.doMovementAnimation = moveAnimations;
        chainManager.doRotationAnimation = rotateAnimations;

        noteManager.ClearRenderedNotes();
        noteManager.UpdateNoteVisuals(TimeManager.CurrentBeat);

        chainManager.ClearRenderedLinks();
        chainManager.UpdateChainVisuals(TimeManager.CurrentBeat);
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateObjectSettings;
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateObjectSettings;
    }


    private void Start()
    {
        UpdateObjectSettings();
    }
}