using UnityEngine;
using UnityEngine.UI;

public class SettingsCheckBox : MonoBehaviour
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
        
        toggle.isOn = SettingsManager.GetBool(rule);
        toggle.onValueChanged.AddListener(SetValue);
    }


    private void OnDisable()
    {
        toggle.onValueChanged.RemoveAllListeners();
    }
}