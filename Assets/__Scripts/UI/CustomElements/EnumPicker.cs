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
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Image valueContainerImage;
    
    [Space]
    [SerializeField] private Color enabledColor;
    [SerializeField] private Color disabledColor;

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


    public void SetInteractable(bool interactable)
    {
        leftButton.interactable = interactable;
        rightButton.interactable = interactable;

        Color color = interactable ? enabledColor : disabledColor;
        labelText.color = color;
        valueText.color = color;
        valueContainerImage.color = UIColorManager.ColorPalette.standardColor * color;
        
        if(interactable)
        {
            UpdateElements();
        }
    }


    private void UpdateElements()
    {
        valueText.text = ValueNames[Value];

        leftButton.interactable = Value > 0;
        rightButton.interactable = Value < maxValue;
    }
}