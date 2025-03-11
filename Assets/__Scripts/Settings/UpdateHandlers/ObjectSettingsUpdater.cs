using System.Linq;
using UnityEngine;

public class ObjectSettingsUpdater : MonoBehaviour
{
    private ObjectManager objectManager => ObjectManager.Instance;
    private NoteManager noteManager => objectManager.noteManager;
    private BombManager bombManager => objectManager.bombManager;
    private ChainManager chainManager => objectManager.chainManager;
    private ArcManager arcManager => objectManager.arcManager;
    private WallManager wallManager => objectManager.wallManager;

    private static readonly string[] allObjectsSettings = new string[]
    {
        "chromaobjectcolors",
        "moveanimations",
        "rotateanimations",
        "flipanimations",
        "variablenjs",
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

    private static readonly string[] outlineSettings = new string[]
    {
        "highlighterrors",
        "missoutlinecolor",
        "badcutoutlinecolor"
    };


    public void UpdateObjectSettings(string setting)
    {
        bool allSettings = setting == "all";
        if(allSettings || allObjectsSettings.Contains(setting))
        {
            HitSoundManager.ClearScheduledSounds();
            objectManager.jumpManager.UpdateNjs(TimeManager.CurrentBeat);

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
            bool outlineSetting = outlineSettings.Contains(setting);

            if(allSettings || arcSettings.Contains(setting))
            {
                arcManager.ClearRenderedVisuals();
                arcManager.UpdateVisuals();
            }
            if(allSettings || outlineSetting || setting == "simplebombs")
            {
                bombManager.ReloadBombs();
            }
            if(allSettings || outlineSetting || setting == "simplenotes" || setting == "lookanimations" || setting == "notesize")
            {
                HitSoundManager.ClearScheduledSounds();
                noteManager.UpdateMaterials();
                chainManager.ClearRenderedVisuals();
                chainManager.UpdateVisuals();
            }
        }
        
        if(allSettings || setting == "wallopacity")
        {
            wallManager.ReloadWalls();
        }
    }


    private void Start()
    {
        SettingsManager.OnSettingsUpdated += UpdateObjectSettings;
        UpdateObjectSettings("all");
    }
}