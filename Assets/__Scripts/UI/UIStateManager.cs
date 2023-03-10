using System;

public class UIStateManager
{
    private static UIState _currentState;
    public static UIState CurrentState
    {
        get => _currentState;

        set
        {
            _currentState = value;
            OnUIStateChanged?.Invoke(value);
        }
    }

    public static event Action<UIState> OnUIStateChanged;
}


public enum UIState
{
    MapSelection,
    Previewer
}