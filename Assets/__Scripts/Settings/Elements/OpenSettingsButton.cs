using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(SettingsMenu))]
public class OpenSettingsButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image image;
    [SerializeField] private Tooltip tooltip;

    [Space]
    [SerializeField] private Sprite openSprite;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite overrideSprite;

    [Space]
    [SerializeField] private string openTooltip;
    [SerializeField] private string closedTooltip;
    [SerializeField] private string overrideTooltip;

    private SettingsMenu settingsMenu;


    public void UpdateTooltip(bool open)
    {
        if(!open && SettingsManager.UseOverrides)
        {
            image.sprite = overrideSprite;
            tooltip.Text = overrideTooltip;
        }
        else
        {
            image.sprite = open ? openSprite : closedSprite;
            tooltip.Text = open ? openTooltip : closedTooltip;
        }

        tooltip.ForceUpdate();
    }


    private void UpdateSettings(string setting)
    {
        UpdateTooltip(SettingsMenu.Open);
    }


    private void Update()
    {
        if(Input.GetButtonDown("Toggle Options") && button.interactable && !DialogueHandler.DialogueActive)
        {
            settingsMenu.ToggleOpen();
        }
    }


    private void OnEnable()
    {
        settingsMenu = GetComponent<SettingsMenu>();
        SettingsMenu.OnOpenUpdated += UpdateTooltip;

        SettingsManager.OnSettingsUpdated += UpdateSettings;
    }
}