using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphicSettingsUpdater : MonoBehaviour
{
    public void UpdateGraphicsSettings()
    {
        bool vsync = SettingsManager.GetRuleBool("vsync");

        QualitySettings.vSyncCount = vsync ? 1 : 0;

        if(!vsync)
        {
            int framecap = SettingsManager.GetRuleInt("framecap");

            //Value of -1 uncaps the framerate
            if(framecap == 0 || framecap > 240) framecap = -1;

            Application.targetFrameRate = framecap;
        }
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateGraphicsSettings;
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateGraphicsSettings;
    }
}