using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsSlider : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private string minOverride;
    [SerializeField] private string maxOverride;
    [SerializeField] private string rule;
    [SerializeField] private bool integerValue;
    [SerializeField] private bool hideInWebGL;

    private Slider slider;


    public void UpdateValue(float value)
    {
        if(!integerValue)
        {
            SettingsManager.SetRule(rule, value);
        }
        else SettingsManager.SetRule(rule, (int)value);

        UpdateText(value);
    }


    public void UpdateText(float value)
    {
        if(value > slider.maxValue - 0.005 && maxOverride != "")
        {
            labelText.text = maxOverride;
        }
        else if(value < slider.minValue + 0.005 && minOverride != "")
        {
            labelText.text = minOverride;
        }
        else labelText.text = Math.Round(value, 2).ToString();  
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

            slider.value = newValue;
            UpdateText(slider.value);
        }
        else if(integerValue)
        {
            int newValue = SettingsManager.GetInt(rule);

            slider.value = newValue;
            UpdateText(slider.value);
        }

        slider.onValueChanged.AddListener(UpdateValue);
    }


    private void OnDisable()
    {
        if(slider)
        {
            slider.onValueChanged.RemoveAllListeners();
        }
    }
}