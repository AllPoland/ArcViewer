using UnityEngine;

public class SettingsEnumPicker : MonoBehaviour
{
    [SerializeField] private string rule;
    [SerializeField] private bool hideInWebGL;

    private EnumPicker enumPicker;


    public void SetRule(int value)
    {
        SettingsManager.SetRule(rule, value);
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

        enumPicker.OnValueChanged += SetRule;
        enumPicker.InitializeValue(SettingsManager.GetInt(rule));
    }
}