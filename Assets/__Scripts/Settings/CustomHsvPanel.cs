using UnityEngine;
using TMPro;

public class CustomHsvPanel : MonoBehaviour
{
    [SerializeField] private HsvLoader hsvLoader;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private GameObject errorIcon;

    [Space]
    [SerializeField] private string hsvRepoURL;


    public void SetConfig(string configJson) => hsvLoader.SetHSV(configJson);


    public void OpenInfoURL()
    {
        Application.OpenURL(hsvRepoURL);
    }


    private void UpdateErrorIcon()
    {
        errorIcon.SetActive(!HsvLoader.ValidConfig);
    }


    private void OnEnable()
    {
        inputField.SetTextWithoutNotify(HsvLoader.CustomConfigJson ?? "");
        UpdateErrorIcon();

        HsvLoader.OnCustomHSVUpdated += UpdateErrorIcon;
    }


    private void OnDisable()
    {
        HsvLoader.OnCustomHSVUpdated -= UpdateErrorIcon;
    }
}