using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public bool Open;

    private SettingsTab _currentTab;
    public SettingsTab CurrentTab
    {
        get => _currentTab;
        set
        {
            _currentTab = value;
            OnTabUpdated?.Invoke(value);
        }
    }

    public event Action<SettingsTab> OnTabUpdated;

    [SerializeField] private GameObject settingsContainer;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private Button openButton;
    [SerializeField] private float panelWidth;
    [SerializeField] private float buttonWidth;
    [SerializeField] private float transitionTime;

    private SettingsManager settingsManager;
    private Vector2 closedPosition;
    private RectTransform containerTransform;
    private bool moving;


    private IEnumerator MovePanelCoroutine(Vector2 startPosition, Vector2 newPosition)
    {
        if(transitionTime == 0)
        {
            containerTransform.anchoredPosition = newPosition;
            settingsPanel.SetActive(Open);
            yield break;
        }

        moving = true;
        openButton.interactable = false;

        if(Open)
        {
            settingsPanel.SetActive(true);
            CurrentTab = SettingsTab.General;
        }

        float t = 0;
        while(t < 1)
        {
            t += Time.deltaTime / transitionTime;
            containerTransform.anchoredPosition = Vector2.Lerp(startPosition, newPosition, Easings.Quad.Out(t));

            yield return null;
        }
        containerTransform.anchoredPosition = newPosition;

        if(!Open) settingsPanel.SetActive(false);

        moving = false;
        openButton.interactable = true;
    }


    public void ToggleOpen()
    {
        if(moving)
        {
            return;
        }

        Open = !Open;

        if(Open)
        {
            SetOpen();
        }
        else SetClosed();
    }


    private void SetOpen()
    {
        Vector2 openPosition = new Vector2(closedPosition.x - panelWidth, closedPosition.y);
        StartCoroutine(MovePanelCoroutine(closedPosition, openPosition));
    }


    private void SetClosed()
    {
        settingsManager.SaveSettings();

        Vector2 openPosition = new Vector2(closedPosition.x - panelWidth, closedPosition.y);
        StartCoroutine(MovePanelCoroutine(openPosition, closedPosition));
    }


    private void Start()
    {
        settingsManager = GetComponent<SettingsManager>();

        containerTransform = settingsContainer.GetComponent<RectTransform>();
        closedPosition = containerTransform.anchoredPosition;
    }
}


public enum SettingsTab
{
    General,
    Appearance,
    Graphics
}