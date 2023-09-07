using System;
using UnityEngine;

public class UIHideInput : MonoBehaviour
{
    private static bool _uiVisible = true;
    public static bool UIVisible
    {
        get => _uiVisible;
        private set
        {
            _uiVisible = value;
            OnUIVisibleChanged?.Invoke(_uiVisible);
        }
    }

    public static event Action<bool> OnUIVisibleChanged;


    private void SetUIVisible(bool visible)
    {
        if(visible != UIVisible)
        {
            UIVisible = visible;
        }
    }


    private void ToggleUI()
    {
        SetUIVisible(!UIVisible);
    }


    private void CheckInput()
    {
        if(Input.GetButtonDown("ToggleUI"))
        {
            ToggleUI();
        }
    }


    private void Update()
    {
        CheckInput();
    }


    private void OnDisable()
    {
        SetUIVisible(true);
    }
}