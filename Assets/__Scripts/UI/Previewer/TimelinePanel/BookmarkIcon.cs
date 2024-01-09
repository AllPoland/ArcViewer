using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BookmarkIcon : MonoBehaviour
{
    public RectTransform rectTransform;

    [SerializeField] private float bookmarkTime;

    [Space]
    [SerializeField] private Image image;
    [SerializeField] private Tooltip tooltip;

    private Canvas parentCanvas;
    private RectTransform parentRectTransform;


    public void OnPointerDown(PointerEventData eventData)
    {
        bool wasPlaying = TimeManager.Playing;
        
        TimeManager.SetPlaying(false);
        TimeManager.CurrentTime = bookmarkTime;
        TimeManager.SetPlaying(wasPlaying);
    }


    private void UpdatePosition()
    {
        float timeSeconds = bookmarkTime / TimeManager.BaseBPM * 60;
        float timeProgress = timeSeconds / SongManager.GetSongLength();;
        float sliderPixelWidth = parentRectTransform.rect.width * parentCanvas.scaleFactor;

        float targetPos = timeProgress * sliderPixelWidth;
        rectTransform.anchoredPosition = new Vector2(targetPos / parentCanvas.scaleFactor, 51f);
    }


    public void SetData(float time, string name, Color color)
    {
        bookmarkTime = time;
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
        rectTransform.sizeDelta = new Vector2(30, 30);
        ScreenSizeHelper.OnScreenSizeChanged += UpdatePosition;
    }


    private void OnDisable()
    {
        ScreenSizeHelper.OnScreenSizeChanged -= UpdatePosition;
    }
}