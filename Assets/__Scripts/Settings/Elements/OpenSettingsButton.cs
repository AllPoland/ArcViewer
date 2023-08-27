using UnityEngine;
using UnityEngine.UI;

public class OpenSettingsButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image image;
    [SerializeField] private Tooltip tooltip;

    [Space]
    [SerializeField] private Sprite openSprite;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private string openTooltip;
    [SerializeField] private string closedTooltip;

    private SettingsMenu settingsMenu;


    public void UpdateTooltip()
    {
        image.sprite = SettingsMenu.Open ? openSprite : closedSprite;
        tooltip.Text = SettingsMenu.Open ? openTooltip : closedTooltip;
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