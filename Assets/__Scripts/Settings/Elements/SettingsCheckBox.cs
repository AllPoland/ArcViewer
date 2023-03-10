using UnityEngine;
using UnityEngine.UI;

public class SettingsCheckBox : MonoBehaviour
{
    [SerializeField] private string rule;
    [SerializeField] private bool hideInWebGL;

    private Toggle toggle;
    

    public void SetValue(bool value)
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

        toggle = GetComponent<Toggle>();

        toggle.isOn = SettingsManager.GetBool(rule);
        toggle.onValueChanged.AddListener(SetValue);
    }


    private void OnDisable()
    {
        if(toggle)
        {
            toggle.onValueChanged.RemoveAllListeners();
        }
    }
}