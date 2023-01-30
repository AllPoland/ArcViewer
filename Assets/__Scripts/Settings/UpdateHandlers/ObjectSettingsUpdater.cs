using UnityEngine;

public class ObjectSettingsUpdater : MonoBehaviour
{
    [SerializeField] private NoteManager noteManager;
    [SerializeField] private ChainManager chainManager;
    [SerializeField] private ArcManager arcManager;

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

        ArcManager.ArcSegmentDensity = SettingsManager.GetInt("arcdensity");

        arcManager.UpdateMaterials();
        arcManager.ClearRenderedArcs();
        arcManager.UpdateArcVisuals(TimeManager.CurrentBeat);
    }


    private void Awake()
    {
        objectManager = ObjectManager.Instance;
    }


    private void Start()
    {
        UpdateObjectSettings();
    }
}