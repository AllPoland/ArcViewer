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
            _value = Mathf.Clamp(value, 0, ValueNames.Length -1);
            OnValueChanged?.Invoke(value);
            UpdateElements();
        }
    }

    public string[] ValueNames;

    public Action<int> OnValueChanged;

    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private TextMeshProUGUI valueText;

    
    public void IncreaseValue()
    {
        if(Value >= ValueNames.Length - 1) return;
        Value++;
    }


    public void DecreaseValue()
    {
        if(Value <= 0) return;
        Value--;
    }


    private void UpdateElements()
    {
        valueText.text = ValueNames[Value];

        leftButton.interactable = Value > 0;
        rightButton.interactable = Value < ValueNames.Length - 1;
    }
}