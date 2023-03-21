using UnityEngine;
using UnityEngine.UI;

public class UISettingsUpdater : MonoBehaviour
{
    [SerializeField] private Canvas canvas;

    private CanvasScaler canvasScaler;
    private float defaultReferenceHeight;


    public void UpdateUISettings()
    {
        float newReferenceHeight = defaultReferenceHeight * (1 / SettingsManager.GetFloat("uiscale"));
        if(newReferenceHeight != canvasScaler.referenceResolution.y)
        {
            canvasScaler.referenceResolution = new Vector2(canvasScaler.referenceResolution.x, newReferenceHeight);
        }

        FileCache.MaxCacheSize = SettingsManager.GetInt("cachesize");
    }


    private void Start()
    {
        canvasScaler = canvas.GetComponent<CanvasScaler>();
        defaultReferenceHeight = canvasScaler.referenceResolution.y;
        SettingsManager.OnSettingsUpdated += UpdateUISettings;

        UpdateUISettings();
    }
}