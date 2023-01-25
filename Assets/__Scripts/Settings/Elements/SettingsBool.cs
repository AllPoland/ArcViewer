using UnityEngine;
using UnityEngine.UI;

public class SettingsBool : MonoBehaviour
{
    [SerializeField] private string rule;

    private Toggle toggle;
    

    public void SetValue(bool value)
    {
        SettingsManager.AddRule(rule, value);
    }


    private void OnEnable()
    {
        toggle = GetComponent<Toggle>();

        if(SettingsManager.CurrentSettings?.Bools != null)
        {
            bool newValue = SettingsManager.GetRuleBool(rule);
            toggle.isOn = newValue;
        }
    }
}