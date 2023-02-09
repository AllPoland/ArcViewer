using UnityEngine;

public class SettingsEnumPicker : MonoBehaviour
{
    [SerializeField] private string rule;

    private EnumPicker enumPicker;


    public void SetRule(int value)
    {
        SettingsManager.SetRule(rule, value);
    }


    private void OnEnable()
    {
        enumPicker = GetComponent<EnumPicker>();

        enumPicker.OnValueChanged += SetRule;

        if(SettingsManager.CurrentSettings?.Ints != null)
        {
            enumPicker.InitializeValue(SettingsManager.GetInt(rule));
        }
    }
}