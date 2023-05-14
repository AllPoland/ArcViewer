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
            if(tabObject.Object)
            {
                tabObject.Object.SetActive(newTab == tabObject.Tab);
            }
        }
    }


    private void OnEnable()
    {
        settingsMenu.OnTabUpdated += UpdateTab;
    }
}


[Serializable]
public struct SettingsTabObject
{
    public SettingsTab Tab;
    public GameObject Object;
}


public enum SettingsTab
{
    General,
    Visuals,
    Graphics,
    Colors,
    Advanced
}