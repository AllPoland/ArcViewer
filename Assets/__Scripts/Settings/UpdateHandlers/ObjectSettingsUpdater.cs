using UnityEngine;

public class ObjectSettingsUpdater : MonoBehaviour
{
    [SerializeField] private NoteManager noteManager;
    [SerializeField] private ChainManager chainManager;

    private ObjectManager objectManager;


    public void UpdateObjectSettings()
    {
        bool simpleNotes = SettingsManager.GetBool("simplenotes");
        bool moveAnimations = SettingsManager.GetBool("moveanimations");
        bool rotateAnimations = SettingsManager.GetBool("rotateanimations");

        objectManager.useSimpleNoteMaterial = simpleNotes;
        objectManager.doMovementAnimation = moveAnimations;
        objectManager.doRotationAnimation = rotateAnimations;

        noteManager.ClearRenderedNotes();
        noteManager.UpdateNoteVisuals(TimeManager.CurrentBeat);

        chainManager.ClearRenderedLinks();
        chainManager.UpdateChainVisuals(TimeManager.CurrentBeat);
    }


    private void OnEnable()
    {
        objectManager = ObjectManager.Instance;
    }


    private void Start()
    {
        UpdateObjectSettings();
    }
}