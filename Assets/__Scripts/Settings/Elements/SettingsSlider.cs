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

    private Slider slider;


    public void UpdateValue(float value)
    {
        if(!integerValue)
        {
            SettingsManager.SetRule(rule, value);
        }
        else SettingsManager.SetRule(rule, (int)value);

        labelText.text = Math.Round(value, 2).ToString();   
    }


    private void OnEnable()
    {
        slider = GetComponent<Slider>();

        if(!integerValue && SettingsManager.CurrentSettings?.Floats != null)
        {
            float newValue = SettingsManager.GetFloat(rule);

            slider.value = newValue;
            UpdateValue(slider.value);
        }
        else if(integerValue && SettingsManager.CurrentSettings?.Ints != null)
        {
            int newValue = SettingsManager.GetInt(rule);

            slider.value = newValue;
            UpdateValue(slider.value);
        }
    }
}