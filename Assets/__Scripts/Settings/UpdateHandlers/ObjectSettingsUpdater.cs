using UnityEngine;

public class ObjectSettingsUpdater : MonoBehaviour
{
    [SerializeField] private NoteManager noteManager;


    public void UpdateObjectSettings()
    {
        noteManager.useSimpleNoteMaterial = SettingsManager.GetBool("simplenotes");
        noteManager.doMovementAnimation = SettingsManager.GetBool("moveanimations");
        noteManager.doRotationAnimation = SettingsManager.GetBool("rotateanimations");

        noteManager.ClearRenderedNotes();
        noteManager.UpdateNoteVisuals(TimeManager.CurrentBeat);
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