using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SettingsTabUpdater : MonoBehaviour
{
    [SerializeField] private SettingsMenu settingsMenu;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private List<SettingsTabObject> tabObjects;


    public void UpdateTab(SettingsTab newTab)
    {
        titleText.text = newTab.ToString();

        foreach(SettingsTabObject tabObject in tabObjects)
        {
            bool enableTab = newTab == tabObject.Tab;

            tabObject.Handler.gameObject.SetActive(enableTab);
            if(enableTab)
            {
                tabObject.Handler.ResetScroll();
            }
        }
    }


    private void Start()
    {
        settingsMenu.OnTabUpdated += UpdateTab;
        UpdateTab(settingsMenu.CurrentTab);
    }
}


[Serializable]
public struct SettingsTabObject
{
    public SettingsTab Tab;
    public SettingsTabHandler Handler;
}


public enum SettingsTab
{
    General,
    Visuals,
    Graphics,
    Replays,
    Colors,
    Advanced
}