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

        //Settings that modify assets should be placed below this,
        //so that they don't get permanently altered by settings
        if(Application.isEditor) return;

        wallManager.UpdateMaterial(SettingsManager.GetFloat("wallopacity"));
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