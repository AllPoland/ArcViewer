using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public bool Open;

    [SerializeField] private GameObject settingsContainer;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button openButton;
    [SerializeField] private float panelWidth;
    [SerializeField] private float buttonWidth;
    [SerializeField] private float transitionTime;

    private SettingsManager settingsManager;
    private Vector2 closedPosition;
    private bool moving;


    private IEnumerator MovePanelCoroutine(Vector2 startPosition, Vector2 newPosition)
    {
        if(transitionTime == 0)
        {
            settingsContainer.transform.localPosition = newPosition;
            settingsPanel.SetActive(Open);
            yield break;
        }

        moving = true;
        openButton.interactable = false;

        if(Open) settingsPanel.SetActive(true);

        float t = 0;
        while(t < 1)
        {
            t += Time.deltaTime / transitionTime;
            settingsContainer.transform.localPosition = Vector2.Lerp(startPosition, newPosition, t);

            yield return null;
        }

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

        closedPosition = settingsContainer.transform.localPosition;
    }
}