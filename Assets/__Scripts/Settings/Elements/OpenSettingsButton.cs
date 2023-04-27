using UnityEngine;
using UnityEngine.UI;

public class OpenSettingsButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Tooltip tooltip;

    private SettingsMenu settingsMenu;


    public void UpdateTooltip()
    {
        tooltip.Text = SettingsMenu.Open ? "Close settings" : "Settings";
    }


    private void Update()
    {
        if(Input.GetButtonDown("Toggle Options") && button.interactable && !DialogueHandler.DialogueActive)
        {
            settingsMenu.ToggleOpen();
            UpdateTooltip();
        }
    }


    private void OnEnable()
    {
        settingsMenu = GetComponent<SettingsMenu>();
    }
}