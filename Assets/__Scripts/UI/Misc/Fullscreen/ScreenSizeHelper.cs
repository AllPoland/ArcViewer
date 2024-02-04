using System;
using UnityEngine;

public class ScreenSizeHelper : MonoBehaviour
{
    public static event Action OnScreenSizeChanged;

    private static float lastScreenWidth;
    private static float lastScreenHeight;

    private static bool forceUpdate = false;
    private static bool didSecondUpdate = false;


    private void UpdateSettings(string setting)
    {
        if(setting == "uiscale")
        {
            forceUpdate = true;
            didSecondUpdate = false;
        }
    }


    private void Update()
    {
        if(forceUpdate || Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            OnScreenSizeChanged?.Invoke();
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            didSecondUpdate = false;
            forceUpdate = false;
        }
        else if(!didSecondUpdate)
        {
            //Invoke a second time a frame later because fullscreen
            //takes a bit to get situated
            OnScreenSizeChanged?.Invoke();
            didSecondUpdate = true;
        }
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
    }
}