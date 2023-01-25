using UnityEngine;

public class TabButton : MonoBehaviour
{
    public SettingsTab targetTab;

    [SerializeField] private SettingsMenu settingsMenu;


    public void SetTargetTab()
    {
        if(settingsMenu.CurrentTab != targetTab)
        {
            settingsMenu.CurrentTab = targetTab;
        }
    }
}