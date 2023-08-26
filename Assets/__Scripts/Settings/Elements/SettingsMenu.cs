using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public static bool Open { get; private set; }

    private SettingsTab _currentTab = SettingsTab.General;
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

    [SerializeField] private RectTransform settingsContainer;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private RectTransform tabButtons;
    [SerializeField] private Button openButton;
    [SerializeField] private float panelWidth;
    [SerializeField] private float buttonWidth;
    [SerializeField] private float transitionTime;

    private SettingsManager settingsManager;
    private Vector2 closedPosition;
    private Vector2 tabButtonPositions;
    private bool moving;
    private IdleHide openButtonHider;


    private IEnumerator MovePanelCoroutine(Vector2 startPosition, Vector2 newPosition)
    {
        if(transitionTime == 0)
        {
            settingsContainer.anchoredPosition = newPosition;
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
            tabButtons.gameObject.SetActive(true);

            buttonStartPos.x += buttonWidth;
        }
        else buttonEndPos.x += buttonWidth;

        float t = 0;
        while(t < 1)
        {
            t += Time.deltaTime / (transitionTime / 2);

            float progress = Easings.Sine.Out(t);
            settingsContainer.anchoredPosition = Vector2.Lerp(startPosition, newPosition, progress);
            tabButtons.anchoredPosition = Vector2.Lerp(buttonStartPos, buttonEndPos, progress);

            yield return null;
        }
        settingsContainer.anchoredPosition = newPosition;
        tabButtons.anchoredPosition = buttonEndPos;

        if(!Open) 
        {
            settingsPanel.SetActive(false);
        }

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
#if !UNITY_WEBGL || UNITY_EDITOR
        settingsManager.SaveSettings();
#endif

        Vector2 openPosition = new Vector2(closedPosition.x - panelWidth, closedPosition.y);
        StartCoroutine(MovePanelCoroutine(openPosition, closedPosition));
    }


    private void Start()
    {
        settingsManager = GetComponent<SettingsManager>();
        openButtonHider = GetComponent<IdleHide>();

        closedPosition = settingsContainer.anchoredPosition;
        tabButtonPositions = tabButtons.anchoredPosition;

        SettingsManager.OnSettingsReset += Close;

        settingsPanel.SetActive(Open);
    }
}