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
            _value = Mathf.Clamp(value, minValue, maxValue);
            OnValueChanged?.Invoke(_value);
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
    [SerializeField] private TMP_InputField inputField;

    public UnityEvent<float> OnValueChanged;

    
    public void IncreaseValue()
    {
        if(Value >= maxValue) return;
        float newValue = Value + increment;
        Value = (float)Math.Round(newValue, roundDigits);
    }


    public void DecreaseValue()
    {
        if(Value <= minValue) return;
        float newValue = Value - increment;
        Value = (float)Math.Round(newValue, roundDigits);
    }


    public void SetValueString(string newValue)
    {
        if(float.TryParse(newValue, out float parsedValue))
        {
            Value = parsedValue;
        }
        else
        {
            UpdateElements();
        }
        EventSystemHelper.SetSelectedGameObject(null);
    }


    public void SetValueWithoutNotify(float value)
    {
        _value = Mathf.Clamp(value, minValue, maxValue);
        UpdateElements();
    }


    private void UpdateElements()
    {
        float textValue = (float)Math.Round(Value, roundDigits);
        inputField.SetTextWithoutNotify(textValue.ToString());

        leftButton.interactable = Value > minValue;
        rightButton.interactable = Value < maxValue;
    }
}