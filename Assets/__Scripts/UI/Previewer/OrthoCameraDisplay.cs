using UnityEngine;
using UnityEngine.UI;

public class OrthoCameraDisplay : MonoBehaviour
{
    [SerializeField] private GameObject cameraWindow;

    [Space]
    [SerializeField] private Image leftSideImage;
    [SerializeField] private Image backSideImage;
    [SerializeField] private Image rightSideImage;

    [Space]
    [SerializeField] private Color buttonOffColor;
    [SerializeField] private Color buttonOnColor;


    public void SetCameraFaceDirection(int direction)
    {
        SettingsManager.SetRule("orthocameraside", direction);
#if !UNITY_WEBGL || UNITY_EDITOR
        SettingsManager.SaveSettingsStatic();
#endif
    }


    private void UpdateButtonColors(int side)
    {
        bool back = side == 0;
        bool right = side == 1;
        bool left = !back && !right;

        leftSideImage.color = left ? buttonOnColor : buttonOffColor;
        rightSideImage.color = right ? buttonOnColor : buttonOffColor;
        backSideImage.color = back ? buttonOnColor : buttonOffColor;
    }

    
    private void UpdateSettings(string setting)
    {
        bool allSettings = setting == "all";
        if(allSettings || setting == "useorthocamera")
        {
            bool enableCamera = SettingsManager.GetBool("useorthocamera");
            cameraWindow.SetActive(enableCamera);
        }
        if(allSettings || setting == "orthocameraside")
        {
            UpdateButtonColors(SettingsManager.GetInt("orthocameraside"));
        }
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;
        if(SettingsManager.Loaded)
        {
            UpdateSettings("all");
        }
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
    }
}