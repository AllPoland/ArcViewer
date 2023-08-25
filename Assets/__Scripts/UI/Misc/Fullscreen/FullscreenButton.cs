using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FullscreenButton : MonoBehaviour, IPointerDownHandler
{
    [DllImport("__Internal")]
    public static extern void ToggleFullscreenWeb();

    [DllImport("__Internal")]
    public static extern bool GetFullscreen();

    [SerializeField] private Image buttonImage;
    [SerializeField] private Sprite fullscreenSprite;
    [SerializeField] private Sprite noFullscreenSprite;

#if !UNITY_WEBGL || UNITY_EDITOR
    private bool isFullscreen => Screen.fullScreen;
    private int preferredWindowWidth = Screen.width;
    private int preferredWindowHeight = Screen.height;
#else
    private bool isFullscreen => GetFullscreen();
#endif


#if !UNITY_WEBGL || UNITY_EDITOR
    public void ToggleFullscreen()
    {
        if(!isFullscreen)
        {
            //Store the current resolution so we can go back to it when exiting fullscreen
            preferredWindowWidth = Screen.width;
            preferredWindowHeight = Screen.height;

            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(preferredWindowWidth, preferredWindowHeight, FullScreenMode.Windowed);
        }
    }
#endif


    private void UpdateButton()
    {
        buttonImage.sprite = isFullscreen ? noFullscreenSprite : fullscreenSprite;
    }


    public void OnPointerDown(PointerEventData eventData)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        ToggleFullscreen();
#else
        ToggleFullscreenWeb();
#endif
    }


    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if(Input.GetKeyDown(KeyCode.F11))
        {
            ToggleFullscreen();
        }
#endif
    }


    private void OnEnable()
    {
        ScreenSizeHelper.OnScreenSizeChanged += UpdateButton;
    }


    private void OnDisable()
    {
        ScreenSizeHelper.OnScreenSizeChanged -= UpdateButton;
    }
}