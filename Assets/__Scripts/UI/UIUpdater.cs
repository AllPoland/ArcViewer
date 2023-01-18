using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIUpdater : MonoBehaviour
{
    [SerializeField] private GameObject selectionScreen;
    [SerializeField] private GameObject previewScreen;
    [SerializeField] private TMP_InputField directoryField;


    public void UpdateState(UIState newState)
    {
        if(newState == UIState.Previewer)
        {
            directoryField.text = "";
        }

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