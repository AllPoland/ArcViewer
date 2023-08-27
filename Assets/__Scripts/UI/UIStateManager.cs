using System;
using UnityEngine;

public static class UIStateManager
{
    private static UIState _currentState;
    public static UIState CurrentState
    {
        get => _currentState;
        set
        {
            _currentState = value;
            Debug.Log($"UI state {_currentState}");
            OnUIStateChanged?.Invoke(_currentState);
        }
    }

    public static event Action<UIState> OnUIStateChanged;
}


public enum UIState
{
    MapSelection,
    Previewer
}