using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class TabButtonScaler : MonoBehaviour
{
    [SerializeField] private float marginSize = 200f;

    private Canvas parentCanvas;
    private RectTransform rectTransform;


    private void UpdateScreenSize()
    {
        //The tab buttons are sideways, so width is actually vertical on the screen
        float width = (rectTransform.sizeDelta.x + marginSize) * parentCanvas.scaleFactor;
        float screenHeight = Screen.height;

        if(width >= screenHeight)
        {
            //Scale down to fit in the screen
            rectTransform.localScale = Vector3.one * (screenHeight / width);
        }
        else rectTransform.localScale = Vector3.one;
    }


    private void OnEnable()
    {
        if(!parentCanvas)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }
        if(!rectTransform)
        {
            rectTransform = (RectTransform)transform;
        }

        ScreenSizeHelper.OnScreenSizeChanged += UpdateScreenSize;
        UpdateScreenSize();
    }


    private void OnDisable()
    {
        ScreenSizeHelper.OnScreenSizeChanged -= UpdateScreenSize;
    }
}