using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TheSoup : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public const string Rule = "thesoup";

    [SerializeField] private TextMeshProUGUI text;

    [Space]
    [SerializeField] private int requiredClicks;
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color hoveredColor;
    [SerializeField] private Color clickedColor;

    private int clickCount;
    private bool clicked;
    private bool hovered;


    private void ToggleSoup()
    {
        bool newSoup = !SettingsManager.GetBool(Rule);
        SettingsManager.SetRule(Rule, newSoup);

        string popupText = newSoup ? "the soup" : "no soup";
        DialogueHandler.ShowDialogueBox(DialogueBoxType.Ok, popupText);

#if !UNITY_WEBGL || UNITY_EDITOR
        SettingsManager.SaveSettingsStatic();
#endif

        clickCount = 0;
    }


    private void CheckRuleChange()
    {
        if(clickCount >= requiredClicks)
        {
            ToggleSoup();
        }
    }


    private void UpdateColor()
    {
        if(clicked)
        {
            text.color = clickedColor;
        }
        else if(hovered)
        {
            text.color = hoveredColor;
        }
        else text.color = defaultColor;
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        UpdateColor();
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        UpdateColor();
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        clicked = true;
        clickCount++;
        CheckRuleChange();
        UpdateColor();
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        clicked = false;
        UpdateColor();
    }


    private void OnEnable()
    {
        clickCount = 0;
        UpdateColor();
    }


    private void OnDisable()
    {
        hovered = false;
        clicked = false;
    }
}