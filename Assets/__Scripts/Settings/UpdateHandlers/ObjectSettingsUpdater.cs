using UnityEngine;

public class ObjectSettingsUpdater : MonoBehaviour
{
    [SerializeField] private NoteManager noteManager;
    [SerializeField] private ChainManager chainManager;
    [SerializeField] private ArcManager arcManager;
    [SerializeField] private WallManager wallManager;


    public void UpdateObjectSettings(string setting)
    {
        bool allSettings = setting == "all";
        if(allSettings || setting == "simplenotes")
        {
            HitSoundManager.ClearScheduledSounds();
            noteManager.UpdateMaterials();
            chainManager.ClearRenderedLinks();
            chainManager.UpdateChainVisuals(TimeManager.CurrentBeat);
        }
        if(allSettings || setting == "moveanimations" || setting == "rotateanimations" || setting == "flipanimations")
        {
            HitSoundManager.ClearScheduledSounds();
            noteManager.ClearRenderedNotes();
            noteManager.UpdateNoteVisuals(TimeManager.CurrentBeat);

            chainManager.ClearRenderedLinks();
            chainManager.UpdateChainVisuals(TimeManager.CurrentBeat);

            arcManager.ClearRenderedArcs();
            arcManager.UpdateArcVisuals(TimeManager.CurrentBeat);
        }
        else if(setting == "arcfadeanimation" || setting == "arctextureanimation" || setting == "arcdensity" || setting == "arcbrightness" || setting == "arcwidth")
        {
            arcManager.ClearRenderedArcs();
            arcManager.UpdateArcVisuals(TimeManager.CurrentBeat);
        }
        
        if(allSettings || setting == "wallopacity")
        {
            wallManager.UpdateMaterial();
        }
    }


    private void Start()
    {
        SettingsManager.OnSettingsUpdated += UpdateObjectSettings;
        UpdateObjectSettings("all");
    }
}