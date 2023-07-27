using System.Linq;
using UnityEngine;

public class ObjectSettingsUpdater : MonoBehaviour
{
    private NoteManager noteManager => ObjectManager.Instance.noteManager;
    private BombManager bombManager => ObjectManager.Instance.bombManager;
    private ChainManager chainManager => ObjectManager.Instance.chainManager;
    private ArcManager arcManager => ObjectManager.Instance.arcManager;
    private WallManager wallManager => ObjectManager.Instance.wallManager;

    private static readonly string[] allObjectsSettings = new string[]
    {
        "moveanimations",
        "rotateanimations",
        "flipanimations",
        "playerheight",
        "accuratereplays"
    };

    private static readonly string[] arcSettings = new string[]
    {
        "arcfadeanimation",
        "arctextureanimation",
        "arcdensity",
        "arcbrightness",
        "arcwidth"
    };


    public void UpdateObjectSettings(string setting)
    {
        bool allSettings = setting == "all";
        if(allSettings || allObjectsSettings.Contains(setting))
        {
            HitSoundManager.ClearScheduledSounds();
            noteManager.ClearRenderedVisuals();
            noteManager.UpdateVisuals();

            bombManager.ReloadBombs();

            chainManager.ClearRenderedVisuals();
            chainManager.UpdateVisuals();

            arcManager.ClearRenderedVisuals();
            arcManager.UpdateVisuals();

            wallManager.ClearRenderedVisuals();
            wallManager.UpdateVisuals();
        }
        else
        {
            if(allSettings || arcSettings.Contains(setting))
            {
                arcManager.ClearRenderedVisuals();
                arcManager.UpdateVisuals();
            }
            if(allSettings || setting == "simplebombs")
            {
                bombManager.ReloadBombs();
            }
            if(allSettings || setting == "simplenotes")
            {
                HitSoundManager.ClearScheduledSounds();
                noteManager.UpdateMaterials();
                chainManager.ClearRenderedVisuals();
                chainManager.UpdateVisuals();
            }
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