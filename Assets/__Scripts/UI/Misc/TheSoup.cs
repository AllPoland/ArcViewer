using UnityEngine;
using UnityEngine.EventSystems;

public class TheSoup : MonoBehaviour, IPointerDownHandler
{
    public const string Rule = "thesoup";
    [SerializeField] private int requiredClicks;

    private int clickCount;


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


    public void OnPointerDown(PointerEventData eventData)
    {
        clickCount++;
        CheckRuleChange();
    }


    private void OnEnable()
    {
        clickCount = 0;
    }
}