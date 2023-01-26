using UnityEngine;
using UnityEngine.UI;

public class SettingsBool : MonoBehaviour
{
    [SerializeField] private string rule;

    private Toggle toggle;
    

    public void SetValue(bool value)
    {
        SettingsManager.SetRule(rule, value);
    }


    private void OnEnable()
    {
        toggle = GetComponent<Toggle>();

        if(SettingsManager.CurrentSettings?.Bools != null)
        {
            bool newValue = SettingsManager.GetBool(rule);
            toggle.isOn = newValue;
        }
    }
}