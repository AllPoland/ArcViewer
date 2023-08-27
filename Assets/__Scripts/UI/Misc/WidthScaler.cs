using UnityEngine;

public class WidthScaler : MonoBehaviour
{
    [SerializeField] private float preferredPadding;
    [SerializeField] private float minWidth;

    private RectTransform rectTransform;
    private RectTransform parentTransform;


    private void UpdateWidth()
    {
        float preferredWidth = parentTransform.rect.width - (preferredPadding * 2);

        Vector2 sizeDelta = rectTransform.sizeDelta;
        if(preferredWidth >= minWidth)
        {
            sizeDelta.x = preferredWidth;
        }
        else
        {
            sizeDelta.x = Mathf.Min(minWidth, parentTransform.rect.width);
        }
        rectTransform.sizeDelta = sizeDelta;
    }


    private void OnEnable()
    {
        if(!rectTransform)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        if(!parentTransform)
        {
            parentTransform = transform.parent.GetComponent<RectTransform>();
        }

        rectTransform.anchorMin = new Vector2(0.5f, rectTransform.anchorMin.y);
        rectTransform.anchorMax = new Vector2(0.5f, rectTransform.anchorMax.y);

        ScreenSizeHelper.OnScreenSizeChanged += UpdateWidth;
        UpdateWidth();
    }


    private void OnDisable()
    {
        ScreenSizeHelper.OnScreenSizeChanged -= UpdateWidth;
    }
}