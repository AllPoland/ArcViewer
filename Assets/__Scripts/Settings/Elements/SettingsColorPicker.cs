using UnityEngine;

[RequireComponent(typeof(ColorPicker))]
public class SettingsColorPicker : MonoBehaviour
{
    [SerializeField] private string rule;
    [SerializeField] private bool hideInWebGL;
    [SerializeField] private Optional<SerializedOption<bool>> requiredSetting = new Optional<SerializedOption<bool>>(new SerializedOption<bool>(), false);

    private ColorPicker colorPicker;


    public void SetValue(Color value)
    {
        SettingsManager.SetRule(rule, value);
    }


    public void UpdateSettings(string changedSetting)
    {
        SerializedOption<bool> option = requiredSetting.Value;
        if(changedSetting == "all" || changedSetting == option.Name)
        {
            colorPicker.Interactable = option.Value == SettingsManager.GetBool(option.Name);
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

        if(!colorPicker)
        {
            colorPicker = GetComponent<ColorPicker>();
        }

        colorPicker.Value = SettingsManager.GetColor(rule);
        colorPicker.OnValueChanged.AddListener(SetValue);

        if(requiredSetting.Enabled)
        {
            SettingsManager.OnSettingsUpdated += UpdateSettings;
            UpdateSettings("all");
        }
    }


    private void OnDisable()
    {
        if(colorPicker)
        {
            colorPicker.OnValueChanged.RemoveAllListeners();
        }
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
    }
}