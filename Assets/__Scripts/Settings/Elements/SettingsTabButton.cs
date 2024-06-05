using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SettingsTabButton : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private SettingsMenu settingsMenu;
    [SerializeField] private UIColorUpdater colorUpdater;

    [Header("Configuration")]
    [SerializeField] public SettingsTab targetTab;
    [SerializeField] private UIColorType inactiveColorType;
    [SerializeField] private UIColorType activeColorType;
    [SerializeField] private float inactiveWidth = 50f;
    [SerializeField] private float activeWidth = 70f;

    private RectTransform rectTransform;


    public void SetTargetTab()
    {
        if(settingsMenu.CurrentTab != targetTab)
        {
            settingsMenu.CurrentTab = targetTab;
        }
    }


    private void UpdateTab(SettingsTab newTab)
    {
        bool active = newTab == targetTab;

        UIColorType newColorType = active ? activeColorType : inactiveColorType;
        colorUpdater.SetColorType(newColorType);

        float newWidth = active ? activeWidth : inactiveWidth;
        rectTransform.sizeDelta = new Vector2(newWidth, rectTransform.sizeDelta.y);
    }


    private void OnEnable()
    {
        rectTransform = (RectTransform)transform;
        settingsMenu.OnTabUpdated += UpdateTab;

        UpdateTab(settingsMenu.CurrentTab);
    }


    private void OnDisable()
    {
        settingsMenu.OnTabUpdated -= UpdateTab;
    }
}