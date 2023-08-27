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
        if(!integerValue)
        {
            SettingsManager.SetRule(rule, value);
        }
        else SettingsManager.SetRule(rule, Mathf.RoundToInt(value));
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
            slider.SetValueWithoutNotify(number);

            SetValue(number);
            UpdateText(number);
        }
        else UpdateText(slider.value);
        EventSystemHelper.SetSelectedGameObject(null);
    }


    public void UpdateSettings(string changedSetting)
    {
        SerializedOption<bool> option = requiredSetting.Value;
        if(changedSetting == "all" || changedSetting == option.Name)
        {
            slider.interactable = option.Value == SettingsManager.GetBool(option.Name);
            valueInput.interactable = slider.interactable;
            Color textColor = slider.interactable ? enabledColor : disabledColor;
            nameLabel.color = textColor;
        }
    }


    public void UpdateText(float value)
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

        slider.wholeNumbers = integerValue;
        slider.minValue = minValue;
        slider.maxValue = maxValue;

        if(integerValue)
        {
            int newValue = SettingsManager.GetInt(rule);

            slider.SetValueWithoutNotify(newValue);
            UpdateText(slider.value);
        }
        else
        {
            float newValue = SettingsManager.GetFloat(rule);

            slider.SetValueWithoutNotify(newValue);
            UpdateText(slider.value);
        }

        slider.onValueChanged.AddListener(UpdateText);

        if(realTimeUpdates)
        {
            slider.onValueChanged.AddListener(SetValue);
        }
        else
        {
            pointerUpHandler.OnSliderEnd.AddListener(SetValue);
        }

        if(requiredSetting.Enabled)
        {
            SettingsManager.OnSettingsUpdated += UpdateSettings;
            UpdateSettings("all");
        }
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