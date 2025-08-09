using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsSlider : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_InputField valueInput;
    [SerializeField] private RectTransform valueText;
    [SerializeField] private TextMeshProUGUI nameLabel;

    [Header("Configuration")]
    [SerializeField] private string minOverride;
    [SerializeField] private string maxOverride;
    [SerializeField] private string rule;

    [Space]
    [SerializeField] private bool integerValue;
    [SerializeField] private Optional<float> stepAmount;
    [SerializeField] private float minValue = 0f;
    [SerializeField] private float maxValue = 1f;

    [Space]
    [SerializeField] private bool hideInWebGL;
    [SerializeField] private bool realTimeUpdates = true;
    [SerializeField] private Optional<SerializedOption<bool>> requiredSetting;

    [Space]
    [SerializeField] private Color enabledColor;
    [SerializeField] private Color disabledColor;

    private SliderPointerUpHandler pointerUpHandler;


    public void SetValue(float value)
    {
        UpdateValue(GetSliderValue());
    }


    public void SetValueText(float value)
    {
        UpdateText(GetSliderValue());
    }


    public void SetValue(string value)
    {
        if(float.TryParse(value, out float number))
        {
            number = Mathf.Clamp(number, minValue, maxValue);

            if(integerValue)
            {
                number = Mathf.RoundToInt(number);
            }
            SetSliderValue(number);

            UpdateValue(number);
            UpdateText(number);
        }
        else UpdateText(GetSliderValue());

        //Force de-select the text field
        EventSystemHelper.SetSelectedGameObject(null);
    }


    private void UpdateSettings(string changedSetting)
    {
        if(changedSetting == "all" || changedSetting == rule)
        {
            float newValue = integerValue ? SettingsManager.GetInt(rule, false) : SettingsManager.GetFloat(rule, false);
            SetSliderValue(newValue);
            UpdateText(newValue);
        }

        if(requiredSetting.Enabled)
        {
            CheckRequiredSetting(changedSetting);
        }
    }


    private void CheckRequiredSetting(string changedSetting)
    {
        SerializedOption<bool> option = requiredSetting.Value;
        if(changedSetting == "all" || changedSetting == option.Name)
        {
            slider.interactable = option.Value == SettingsManager.GetBool(option.Name, false);
            valueInput.interactable = slider.interactable;
            Color textColor = slider.interactable ? enabledColor : disabledColor;
            nameLabel.color = textColor;
        }
    }
    

    private void UpdateValue(float value)
    {
        if(integerValue)
        {
            SettingsManager.SetRule(rule, Mathf.RoundToInt(value));
        }
        else SettingsManager.SetRule(rule, value);
    }


    private void UpdateText(float value)
    {
        if(value > maxValue - 0.005 && maxOverride != "")
        {
            valueInput.SetTextWithoutNotify(maxOverride);
        }
        else if(value < minValue + 0.005 && minOverride != "")
        {
            valueInput.SetTextWithoutNotify(minOverride);
        }
        else valueInput.SetTextWithoutNotify(Math.Round(value, 2).ToString());

        valueText.anchoredPosition = Vector2.zero;
    }


    private void SetSliderValue(float value)
    {
        if(!stepAmount.Enabled)
        {
            slider.SetValueWithoutNotify(value);
            return;
        }

        float convertedValue = (value - minValue) / stepAmount.Value;
        slider.SetValueWithoutNotify(convertedValue);
    }


    private float GetSliderValue()
    {
        if(!stepAmount.Enabled)
        {
            return slider.value;
        }

        float sliderValue = (slider.value * stepAmount.Value) + minValue;
        return sliderValue;
    }


    private void OnEnable()
    {
#if UNITY_WEBGL
        if(hideInWebGL)
        {
            gameObject.SetActive(false);
            return;
        }
#endif

        if(!pointerUpHandler)
        {
            pointerUpHandler = slider.GetComponent<SliderPointerUpHandler>();
        }

        slider.wholeNumbers = integerValue || stepAmount.Enabled;
        if(stepAmount.Enabled)
        {
            //Turn the slider into an integer slider, and convert the min and max
            //into an equivalent number of steps
            float valueRange = maxValue - minValue;
            int numSteps = (int)(valueRange / stepAmount.Value);

            slider.minValue = 0;
            slider.maxValue = numSteps;
        }
        else
        {
            slider.minValue = minValue;
            slider.maxValue = maxValue;
        }

        if(integerValue)
        {
            int newValue = SettingsManager.GetInt(rule);

            SetSliderValue(newValue);
            UpdateText(newValue);
        }
        else
        {
            float newValue = SettingsManager.GetFloat(rule);

            SetSliderValue(newValue);
            UpdateText(newValue);
        }

        slider.onValueChanged.AddListener(SetValueText);

        if(realTimeUpdates)
        {
            slider.onValueChanged.AddListener(SetValue);
        }
        else
        {
            pointerUpHandler.OnSliderEnd.AddListener(SetValue);
        }

        SettingsManager.OnSettingsUpdated += UpdateSettings;
        UpdateSettings("all");
    }


    private void OnDisable()
    {
        if(slider)
        {
            slider.onValueChanged.RemoveAllListeners();
        }
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
    }
}