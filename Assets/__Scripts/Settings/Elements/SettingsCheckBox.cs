using UnityEngine;
using UnityEngine.UI;

public class SettingsCheckBox : MonoBehaviour
{
    [SerializeField] private string rule;
    [SerializeField] private bool hideInWebGL;
    [SerializeField] private Optional<SerializedOption<bool>> requiredSetting = new Optional<SerializedOption<bool>>(new SerializedOption<bool>(), false);

    private Toggle toggle;
    

    public void SetValue(bool value)
    {
        SettingsManager.SetRule(rule, value);
    }


    public void UpdateSettings(string changedSetting)
    {
        SerializedOption<bool> option = requiredSetting.Value;
        if(changedSetting == "all" || changedSetting == option.Name)
        {
            toggle.interactable = option.Value == SettingsManager.GetBool(option.Name);
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

        toggle = GetComponent<Toggle>();

        toggle.isOn = SettingsManager.GetBool(rule);
        toggle.onValueChanged.AddListener(SetValue);

        if(requiredSetting.Enabled)
        {
            SettingsManager.OnSettingsUpdated += UpdateSettings;
            UpdateSettings("all");
        }
    }


    private void OnDisable()
    {
        if(toggle)
        {
            toggle.onValueChanged.RemoveAllListeners();
        }
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
    }
}