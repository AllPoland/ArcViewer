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
        if(hideInWebGL && Application.platform == RuntimePlatform.WebGLPlayer)
        {
            gameObject.SetActive(false);
            return;
        }

        enumPicker = GetComponent<EnumPicker>();

        enumPicker.OnValueChanged += SetRule;
        enumPicker.InitializeValue(SettingsManager.GetInt(rule));
    }
}