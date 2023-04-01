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
#else
    private bool isFullscreen => GetFullscreen();
#endif


    public void ToggleFullscreen()
    {
        Screen.fullScreenMode = FullScreenMode.MaximizedWindow;
        Screen.fullScreen = !isFullscreen;
    }


    private void UpdateButton()
    {
        Sprite newSprite = isFullscreen ? noFullscreenSprite : fullscreenSprite;
        if(newSprite != buttonImage.sprite)
        {
            buttonImage.sprite = newSprite;
        }
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
        UpdateButton();
    }


    private void OnEnable()
    {
        UpdateButton();
    }
}