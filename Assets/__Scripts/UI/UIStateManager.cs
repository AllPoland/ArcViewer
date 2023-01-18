using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIStateManager
{
    private static UIState _currentState;
    public static UIState CurrentState
    {
        get
        {
            return _currentState;
        }
        set
        {
            _currentState = value;
            OnUIStateChanged?.Invoke(value);
        }
    }

    public delegate void UIStateDelegate(UIState state);
    public static event UIStateDelegate OnUIStateChanged;
}


public enum UIState
{
    MapSelection,
    Previewer
}