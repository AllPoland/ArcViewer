using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class ValuePicker : MonoBehaviour
{
    private float _value;
    public float Value
    {
        get => _value;
        set
        {
            _value = (float)Math.Round(Mathf.Clamp(value, minValue, maxValue), roundDigits);
            OnValueChanged?.Invoke(value);
            UpdateElements();
        }
    }

    [Header("Configuration")]
    [SerializeField] private float increment;
    [SerializeField] private float maxValue;
    [SerializeField] private float minValue;
    [SerializeField] private int roundDigits;

    [Header("Elements")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private TextMeshProUGUI valueText;

    public UnityEvent<float> OnValueChanged;

    
    public void IncreaseValue()
    {
        if(Value >= maxValue) return;
        Value += increment;
    }


    public void DecreaseValue()
    {
        if(Value <= minValue) return;
        Value -= increment;
    }


    public void SetValueWithoutNotify(float value)
    {
        _value = (float)Math.Round(Mathf.Clamp(value, minValue, maxValue), roundDigits);
        UpdateElements();
    }


    private void UpdateElements()
    {
        valueText.text = Value.ToString();

        leftButton.interactable = Value > minValue;
        rightButton.interactable = Value < maxValue;
    }
}