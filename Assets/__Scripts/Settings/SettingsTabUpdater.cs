using UnityEngine;
using TMPro;

public class SettingsTabUpdater : MonoBehaviour
{
    [SerializeField] private SettingsMenu settingsMenu;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private GameObject generalMenu;
    [SerializeField] private GameObject visualsMenu;
    [SerializeField] private GameObject graphicsMenu;


    public void UpdateTab(SettingsTab newTab)
    {
        titleText.text = newTab.ToString();

        generalMenu.SetActive(newTab == SettingsTab.General);
        visualsMenu.SetActive(newTab == SettingsTab.Visuals);
        graphicsMenu.SetActive(newTab == SettingsTab.Graphics);
    }


    private void OnEnable()
    {
        settingsMenu.OnTabUpdated += UpdateTab;
    }
}