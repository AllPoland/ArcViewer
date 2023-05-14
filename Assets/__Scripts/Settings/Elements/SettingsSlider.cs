using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SettingsSlider : MonoBehaviour, IPointerUpHandler
{
    [SerializeField] private TextMeshProUGUI valueLabel;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private string minOverride;
    [SerializeField] private string maxOverride;
    [SerializeField] private string rule;
    [SerializeField] private bool integerValue;
    [SerializeField] private bool hideInWebGL;
    [SerializeField] private bool realTimeUpdates = true;
    [SerializeField] private Optional<SerializedOption<bool>> requiredSetting;

    [Space]
    [SerializeField] private Color enabledColor;
    [SerializeField] private Color disabledColor;

    private Slider slider;


    public void OnPointerUp(PointerEventData eventData)
    {
        SetValue(slider.value);
    }


    public void SetValue(float value)
    {
        if(!integerValue)
        {
            SettingsManager.SetRule(rule, value);
        }
        else SettingsManager.SetRule(rule, (int)value);
    }


    public void UpdateSettings(string changedSetting)
    {
        SerializedOption<bool> option = requiredSetting.Value;
        if(changedSetting == "all" || changedSetting == option.Name)
        {
            slider.interactable = option.Value == SettingsManager.GetBool(option.Name);
            Color textColor = slider.interactable ? enabledColor : disabledColor;
            valueLabel.color = textColor;
            nameLabel.color = textColor;
        }
    }


    public void UpdateText(float value)
    {
        if(value > slider.maxValue - 0.005 && maxOverride != "")
        {
            valueLabel.text = maxOverride;
        }
        else if(value < slider.minValue + 0.005 && minOverride != "")
        {
            valueLabel.text = minOverride;
        }
        else valueLabel.text = Math.Round(value, 2).ToString();  
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

        slider = GetComponent<Slider>();

        if(!integerValue)
        {
            float newValue = SettingsManager.GetFloat(rule);

            slider.SetValueWithoutNotify(newValue);
            UpdateText(slider.value);
        }
        else if(integerValue)
        {
            int newValue = SettingsManager.GetInt(rule);

            slider.SetValueWithoutNotify(newValue);
            UpdateText(slider.value);
        }

        slider.onValueChanged.AddListener(UpdateText);

        if(realTimeUpdates)
        {
            slider.onValueChanged.AddListener(SetValue);
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