using UnityEngine;

public class ObjectSettingsUpdater : MonoBehaviour
{
    [SerializeField] private NoteManager noteManager;
    [SerializeField] private ChainManager chainManager;
    [SerializeField] private ArcManager arcManager;
    [SerializeField] private WallManager wallManager;

    private ObjectManager objectManager;


    public void UpdateObjectSettings()
    {
        bool simpleNotes = SettingsManager.GetBool("simplenotes");
        bool moveAnimations = SettingsManager.GetBool("moveanimations");
        bool rotateAnimations = SettingsManager.GetBool("rotateanimations");
        bool flipAnimations = SettingsManager.GetBool("flipanimations");

        objectManager.useSimpleNoteMaterial = simpleNotes;
        objectManager.doMovementAnimation = moveAnimations;
        objectManager.doRotationAnimation = rotateAnimations;
        objectManager.doFlipAnimation = flipAnimations;

        noteManager.ClearRenderedNotes();
        noteManager.UpdateNoteVisuals(TimeManager.CurrentBeat);

        chainManager.ClearRenderedLinks();
        chainManager.UpdateChainVisuals(TimeManager.CurrentBeat);

        ArcManager.ArcSegmentDensity = SettingsManager.GetInt("arcdensity");

        arcManager.UpdateMaterials();
        wallManager.UpdateMaterial();
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