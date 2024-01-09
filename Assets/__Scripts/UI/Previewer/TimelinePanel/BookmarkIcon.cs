using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BookmarkIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public RectTransform rectTransform;

    [SerializeField] private float time;

    [Space]
    [SerializeField] private Image image;
    [SerializeField] private Tooltip tooltip;

    [Space]
    [SerializeField] private Vector2 normalSize;
    [SerializeField] private Vector2 hoveredSize;

    private Canvas parentCanvas;
    private RectTransform parentRectTransform;


    private void UpdatePosition()
    {
        float timeProgress = time / SongManager.GetSongLength();
        float sliderPixelWidth = parentRectTransform.rect.width * parentCanvas.scaleFactor;

        float targetPos = timeProgress * sliderPixelWidth;
        rectTransform.anchoredPosition = new Vector2(targetPos / parentCanvas.scaleFactor, 0f);
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        rectTransform.sizeDelta = hoveredSize;

        Vector2 padding = (hoveredSize - normalSize) / 2;
        image.raycastPadding = new Vector4(padding.x, padding.y, padding.x, padding.y);
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform.sizeDelta = normalSize;
        image.raycastPadding = Vector4.zero;
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        bool wasPlaying = TimeManager.Playing;
        
        TimeManager.SetPlaying(false);
        TimeManager.CurrentTime = time;
        TimeManager.SetPlaying(wasPlaying);
    }


    public void SetData(float beat, string name, Color color)
    {
        time = TimeManager.TimeFromBeat(beat);
        tooltip.Text = name;
        image.color = color;

        UpdatePosition();
    }


    public void SetParentReferences(RectTransform parent, Canvas canvas)
    {
        parentRectTransform = parent;
        parentCanvas = canvas;
    }


    private void OnEnable()
    {
        rectTransform.sizeDelta = normalSize;
        ScreenSizeHelper.OnScreenSizeChanged += UpdatePosition;
    }


    private void OnDisable()
    {
        ScreenSizeHelper.OnScreenSizeChanged -= UpdatePosition;
    }
}