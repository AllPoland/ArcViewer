using UnityEngine;

public class UISettingsUpdater : MonoBehaviour
{
    public void UpdateUISettings()
    {
        FileCache.MaxCacheSize = SettingsManager.GetInt("cachesize");
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateUISettings;
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateUISettings;
    }


    private void Start()
    {
        UpdateUISettings();
    }
}