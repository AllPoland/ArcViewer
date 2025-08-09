using System;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

public class SettingsOverrideExportPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField outputText;


    private void OnEnable()
    {
        try
        {
            Settings exportSettings = SettingsManager.CurrentSettings.ExportOverrides();
            string serializedSettings = JsonConvert.SerializeObject(exportSettings, Formatting.None);
            outputText.text = serializedSettings;
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to serialized settings override with error: {err.Message}, {err.StackTrace}");
            outputText.text = "Failed to export settings! Check logs for more info.";
        }
    }
}