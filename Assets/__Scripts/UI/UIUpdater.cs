using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIUpdater : MonoBehaviour
{
    [SerializeField] private GameObject selectionScreen;
    [SerializeField] private GameObject previewScreen;


    public void UpdateState(UIState newState)
    {
        selectionScreen.SetActive(newState == UIState.MapSelection);
        previewScreen.SetActive(newState == UIState.Previewer);
    }


    private void Start()
    {
        UIStateManager.OnUIStateChanged += UpdateState;
        UIStateManager.CurrentState = UIState.MapSelection;
    }


    private void OnDisable()
    {
        UIStateManager.OnUIStateChanged -= UpdateState;
    }
}