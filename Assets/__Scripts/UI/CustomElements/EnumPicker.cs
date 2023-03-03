using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnumPicker : MonoBehaviour
{
    private int _value;
    public int Value
    {
        get => _value;
        set
        {
            _value = Mathf.Clamp(value, 0, maxValue);
            OnValueChanged?.Invoke(value);
            UpdateElements();
        }
    }

    public string[] ValueNames;

    public Action<int> OnValueChanged;

    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private TextMeshProUGUI valueText;

    private int maxValue => ValueNames.Length - 1;

    
    public void IncreaseValue()
    {
        if(Value >= maxValue) return;
        Value++;
    }


    public void DecreaseValue()
    {
        if(Value <= 0) return;
        Value--;
    }


    public void SetValueWithoutNotify(int value)
    {
        _value = Mathf.Clamp(value, 0, maxValue);
        UpdateElements();
    }


    private void UpdateElements()
    {
        valueText.text = ValueNames[Value];

        leftButton.interactable = Value > 0;
        rightButton.interactable = Value < maxValue;
    }
}