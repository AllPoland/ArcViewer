using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public bool Open { get; private set; }

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
    [SerializeField] private GameObject tabButtons;
    [SerializeField] private Button openButton;
    [SerializeField] private float panelWidth;
    [SerializeField] private float buttonWidth;
    [SerializeField] private float transitionTime;

    private SettingsManager settingsManager;
    private Vector2 closedPosition;
    private Vector2 tabButtonPositions;
    private RectTransform containerTransform;
    private RectTransform tabButtonTransform;
    private bool moving;
    private IdleHide openButtonHider;


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

        Vector2 buttonStartPos = tabButtonPositions;
        Vector2 buttonEndPos = tabButtonPositions;

        if(Open)
        {
            settingsPanel.SetActive(true);
            CurrentTab = SettingsTab.General;

            buttonStartPos.x += buttonWidth;
        }
        else buttonEndPos.x += buttonWidth;

        float t = 0;
        while(t < 1)
        {
            t += Time.deltaTime / (transitionTime / 2);

            float progress = Easings.Sine.Out(t);
            containerTransform.anchoredPosition = Vector2.Lerp(startPosition, newPosition, progress);
            tabButtonTransform.anchoredPosition = Vector2.Lerp(buttonStartPos, buttonEndPos, progress);

            yield return null;
        }
        containerTransform.anchoredPosition = newPosition;
        tabButtonTransform.anchoredPosition = buttonEndPos;

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
        openButtonHider.forceEnable = Open;

        if(Open)
        {
            SetOpen();
        }
        else SetClosed();
    }


    public void Close()
    {
        if(!Open)
        {
            return;
        }

        ToggleOpen();
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
        tabButtonTransform = tabButtons.GetComponent<RectTransform>();

        openButtonHider = GetComponent<IdleHide>();

        closedPosition = containerTransform.anchoredPosition;
        tabButtonPositions = tabButtonTransform.anchoredPosition;

        SettingsManager.OnSettingsReset += Close;

        settingsPanel.SetActive(Open);
    }
}


public enum SettingsTab
{
    General,
    Visuals,
    Graphics,
    Advanced
}