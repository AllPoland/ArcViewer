using UnityEngine;
using UnityEngine.UI;

public class UISettingsUpdater : MonoBehaviour
{
    [SerializeField] private Canvas canvas;

    private CanvasScaler canvasScaler;
    private float defaultReferenceHeight;


    private void UpdateUISettings(string setting)
    {
        bool allSettings = setting == "all";
        if(allSettings || setting == "uiscale")
        {
            float newReferenceHeight = defaultReferenceHeight * (1 / SettingsManager.GetFloat("uiscale"));
            canvasScaler.referenceResolution = new Vector2(canvasScaler.referenceResolution.x, newReferenceHeight);
        }
    }


    private void Start()
    {
        canvasScaler = canvas.GetComponent<CanvasScaler>();
        defaultReferenceHeight = canvasScaler.referenceResolution.y;

        SettingsManager.OnSettingsUpdated += UpdateUISettings;
        if(SettingsManager.Loaded)
        {
            UpdateUISettings("all");
        }
    }
}