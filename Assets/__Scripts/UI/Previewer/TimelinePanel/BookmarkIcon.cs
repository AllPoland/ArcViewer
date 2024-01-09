using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

// this is similar enough to the MistakeIcon script because
// both of them go on the timeline. This might be scuffed though.
public class BookmarkIcon : MonoBehaviour
{
    public RectTransform rectTransform;
    [Header ("Bookmark Info")]
    [SerializeField] private string bookmarkName;
    [SerializeField] private Color color;
    [SerializeField] private float time;
    [UnitHeaderInspectable("Metadata")]
    [SerializeField] private Image image;
    [SerializeField] private Tooltip tooltip;

    private Canvas parentCanvas;
    private RectTransform parentRectTransform;

    private void UpdatePosition()
    {
        float timeSeconds = time / TimeManager.BaseBPM * 60;
        float timeProgress = timeSeconds / SongManager.GetSongLength();;
        float sliderPixelWidth = parentRectTransform.rect.width * parentCanvas.scaleFactor;

        float targetPos = timeProgress * sliderPixelWidth;
        rectTransform.anchoredPosition = new Vector2(targetPos / parentCanvas.scaleFactor, 0f);
    }

    private void UpdateTooltip()
    {
        tooltip.Text = bookmarkName;
    }

    private void UpdateColor()
    {
        image.color = color;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        bool wasPlaying = TimeManager.Playing;
        
        TimeManager.SetPlaying(false);
        TimeManager.CurrentTime = time;
        TimeManager.SetPlaying(wasPlaying);
    }
    public void SetData(float time, string name, Color? color)
    {
        this.time = time;
        bookmarkName = name;
        if(color != null)
            this.color = (Color)color;
        else
            this.color = Random.ColorHSV();

        UpdatePosition();
        UpdateTooltip();
        UpdateColor();
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
