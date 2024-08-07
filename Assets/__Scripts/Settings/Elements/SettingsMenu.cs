using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    private static bool _open;
    public static bool Open
    {
        get => _open;
        private set
        {
            _open = value;
            OnOpenUpdated?.Invoke(_open);
        }
    }

    public static event Action<bool> OnOpenUpdated;

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

    [SerializeField] private RectTransform settingsContainer;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private RectTransform tabButtons;
    [SerializeField] private Button openButton;
    [SerializeField] private IdleHide openButtonHider;
    [SerializeField] private float panelWidth;
    [SerializeField] private float buttonWidth;
    [SerializeField] private float transitionTime;

    private Vector2 closedPosition;
    private Vector2 tabButtonPositions;
    private bool moving;


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
        Vector2 openPosition = new Vector2(closedPosition.x - panelWidth, closedPosition.y);
        StartCoroutine(MovePanelCoroutine(openPosition, closedPosition));
    }


    private void Start()
    {
        closedPosition = settingsContainer.anchoredPosition;
        tabButtonPositions = tabButtons.anchoredPosition;

        SettingsManager.OnSettingsReset += Close;

        settingsPanel.SetActive(Open);
    }
}