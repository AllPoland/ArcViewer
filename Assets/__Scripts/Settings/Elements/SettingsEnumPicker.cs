using UnityEngine;

public class SettingsEnumPicker : MonoBehaviour
{
    [SerializeField] private string rule;
    [SerializeField] private bool hideInWebGL;
    [SerializeField] private Optional<SerializedOption<bool>> requiredSetting;

    private EnumPicker enumPicker;


    public void SetValue(int value)
    {
        SettingsManager.SetRule(rule, value);
    }


    public void UpdateSettings(string changedSetting)
    {
        SerializedOption<bool> option = requiredSetting.Value;
        if(changedSetting == "all" || changedSetting == option.Name)
        {
            enumPicker.SetInteractable(option.Value == SettingsManager.GetBool(option.Name));
        }
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

        enumPicker = GetComponent<EnumPicker>();

        enumPicker.OnValueChanged += SetValue;
        enumPicker.SetValueWithoutNotify(SettingsManager.GetInt(rule));

        if(requiredSetting.Enabled)
        {
            SettingsManager.OnSettingsUpdated += UpdateSettings;
            UpdateSettings("all");
        }
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
    }
}