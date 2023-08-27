using System;
using UnityEngine;

public class ScreenSizeHelper : MonoBehaviour
{
    public static event Action OnScreenSizeChanged;

    private static float lastScreenWidth;
    private static float lastScreenHeight;

    private static bool secondUpdate = false;


    private void Update()
    {
        if(Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            OnScreenSizeChanged?.Invoke();
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            secondUpdate = false;
        }
        else if(!secondUpdate)
        {
            //Invoke a second time a frame later because fullscreen
            //takes a bit to get situated
            OnScreenSizeChanged?.Invoke();
            secondUpdate = true;
        }
    } 
}