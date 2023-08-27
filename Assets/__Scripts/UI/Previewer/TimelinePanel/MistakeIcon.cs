using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MistakeIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public RectTransform rectTransform;

    [SerializeField] private Image image;
    [SerializeField] private Tooltip tooltip;

    [Space]
    [SerializeField] private float normalSize;
    [SerializeField] private float hoveredSize;

    private float eventTime;
    private RectTransform parentRectTransform;
    private Canvas parentCanvas;


    private void UpdatePosition()
    {
        float timeProgress = eventTime / SongManager.GetSongLength();;
        float sliderPixelWidth = parentRectTransform.rect.width * parentCanvas.scaleFactor;

        float targetPos = timeProgress * sliderPixelWidth;
        rectTransform.anchoredPosition = new Vector2(targetPos / parentCanvas.scaleFactor, 0f);
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        rectTransform.sizeDelta = new Vector2(hoveredSize, hoveredSize);

        float padding = (hoveredSize - normalSize) / 2;
        image.raycastPadding = new Vector4(padding, padding, padding, 0);
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform.sizeDelta = new Vector2(normalSize, normalSize);
        image.raycastPadding = Vector4.zero;
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        bool wasPlaying = TimeManager.Playing;
        
        TimeManager.SetPlaying(false);
        TimeManager.CurrentTime = eventTime;
        TimeManager.SetPlaying(wasPlaying);
    }


    public void SetVisual(Sprite sprite, Color color)
    {
        image.sprite = sprite;
        image.color = color;
    }


    public void SetTooltip(string tooltipText)
    {
        tooltip.Text = tooltipText;
    }


    public void SetParentReferences(RectTransform parent, Canvas canvas)
    {
        parentRectTransform = parent;
        parentCanvas = canvas;
    }


    public void SetTime(float time)
    {
        eventTime = time;
        UpdatePosition();        
    }


    private void OnEnable()
    {
        rectTransform.sizeDelta = new Vector2(normalSize, normalSize);
        ScreenSizeHelper.OnScreenSizeChanged += UpdatePosition;
    }


    private void OnDisable()
    {
        ScreenSizeHelper.OnScreenSizeChanged -= UpdatePosition;
    }
}