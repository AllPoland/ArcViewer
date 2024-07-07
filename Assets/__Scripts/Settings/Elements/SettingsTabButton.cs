using UnityEngine;

public class SettingsTabButton : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private SettingsMenu settingsMenu;
    [SerializeField] private UIColorUpdater colorUpdater;

    [Header("Configuration")]
    [SerializeField] public SettingsTab targetTab;
    [SerializeField] private UIColorType inactiveColorType;
    [SerializeField] private UIColorType activeColorType;


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
    }


    private void OnEnable()
    {
        settingsMenu.OnTabUpdated += UpdateTab;
        UpdateTab(settingsMenu.CurrentTab);
    }


    private void OnDisable()
    {
        settingsMenu.OnTabUpdated -= UpdateTab;
    }
}