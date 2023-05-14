using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TooltipDisplay : MonoBehaviour
{
    public bool Active;

    [SerializeField] private GameObject tooltipContainer;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private float heightOffset;
    [SerializeField] private float topHeightOffset;
    [SerializeField] private float screenPadding;

    private Canvas parentCanvas;
    private RectTransform canvasTransform;
    private RectTransform rectTransform;
    private RectTransform containerTransform;
    private Vector2 anchor;


    private void Show(string text)
    {
        Active = true;
        tooltipContainer.SetActive(true);
        tooltipText.text = text;

        //This forces the canvas to update the content size fitters
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipText.rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        
        UpdatePosition();
    }


    private void Hide()
    {
        Active = false;
        tooltipContainer.SetActive(false);
    }


    public void UpdateTooltip(Tooltip newTooltip)
    {
        if(newTooltip == null)
        {
            Hide();
            return;
        }

        Show(newTooltip.Text);
    }


    private void AlignTop()
    {
        anchor.y = 0;
        containerTransform.anchorMax = anchor;
        containerTransform.anchorMin = anchor;
        containerTransform.pivot = new Vector2(containerTransform.pivot.x, anchor.y);
    }


    private void AlignBottom()
    {
        anchor.y = 1;
        containerTransform.anchorMax = anchor;
        containerTransform.anchorMin = anchor;
        containerTransform.pivot = new Vector2(containerTransform.pivot.x, anchor.y);
    }


    private void AlignLeft()
    {
        anchor.x = 0;
        containerTransform.anchorMax = anchor;
        containerTransform.anchorMin = anchor;
        containerTransform.pivot = new Vector2(anchor.x, containerTransform.pivot.y);
    }


    private void AlignRight()
    {
        anchor.x = 1;
        containerTransform.anchorMax = anchor;
        containerTransform.anchorMin = anchor;
        containerTransform.pivot = new Vector2(anchor.x, containerTransform.pivot.y);
    }


    private void UpdatePosition()
    {
        Vector2 containerPosition = containerTransform.anchoredPosition;
        Vector2 mousePosition = Input.mousePosition / parentCanvas.scaleFactor;
        
        //Align the tooltip below the cursor unless it would be off-screen
        float height = containerTransform.sizeDelta.y + heightOffset;
        if(mousePosition.y - height >= screenPadding)
        {
            AlignBottom();
            containerPosition.y = -heightOffset;
        }
        else
        {
            AlignTop();
            containerPosition.y = topHeightOffset;
        }

        //Align the tooltip to so the cursor is on the left side unless it would be off-screen
        float width = containerTransform.sizeDelta.x;
        if(mousePosition.x + width > canvasTransform.sizeDelta.x - screenPadding)
        {
            AlignRight();
        }
        else
        {
            AlignLeft();
        }
        containerPosition.x = 0;

        containerTransform.anchoredPosition = containerPosition;

        //Align the mouse position so 0, 0 is the bottom left corner
        mousePosition.y -= canvasTransform.sizeDelta.y / 2;
        mousePosition.x -= canvasTransform.sizeDelta.x / 2;
        rectTransform.anchoredPosition = mousePosition;
    }


    private void Update()
    {
        if(Active)
        {
            UpdatePosition();
        }
    }


    private void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        canvasTransform = parentCanvas.GetComponent<RectTransform>();
        rectTransform = GetComponent<RectTransform>();
        containerTransform = tooltipContainer.GetComponent<RectTransform>();

        TooltipManager.OnActiveTooltipChanged += UpdateTooltip;

        Hide();
    }
}