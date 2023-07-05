using UnityEngine;

public class ObjectSettingsUpdater : MonoBehaviour
{
    private NoteManager noteManager => ObjectManager.Instance.noteManager;
    private BombManager bombManager => ObjectManager.Instance.bombManager;
    private ChainManager chainManager => ObjectManager.Instance.chainManager;
    private ArcManager arcManager => ObjectManager.Instance.arcManager;
    private WallManager wallManager => ObjectManager.Instance.wallManager;


    public void UpdateObjectSettings(string setting)
    {
        bool allSettings = setting == "all";
        if(allSettings || setting == "simplenotes")
        {
            HitSoundManager.ClearScheduledSounds();
            noteManager.UpdateMaterials();
            chainManager.ClearRenderedVisuals();
            chainManager.UpdateVisuals();
        }
        if(allSettings || setting == "simplebombs")
        {
            bombManager.ReloadBombs();
        }
        if(allSettings || setting == "moveanimations" || setting == "rotateanimations" || setting == "flipanimations")
        {
            HitSoundManager.ClearScheduledSounds();
            noteManager.ClearRenderedVisuals();
            noteManager.UpdateVisuals();

            bombManager.ReloadBombs();

            chainManager.ClearRenderedVisuals();
            chainManager.UpdateVisuals();

            arcManager.ClearRenderedVisuals();
            arcManager.UpdateVisuals();
        }
        else if(setting == "arcfadeanimation" || setting == "arctextureanimation" || setting == "arcdensity" || setting == "arcbrightness" || setting == "arcwidth")
        {
            arcManager.ClearRenderedVisuals();
            arcManager.UpdateVisuals();
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