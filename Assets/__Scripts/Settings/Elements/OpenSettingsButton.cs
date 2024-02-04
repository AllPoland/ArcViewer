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
    [SerializeField] private string openTooltip;
    [SerializeField] private string closedTooltip;

    private SettingsMenu settingsMenu;


    public void UpdateTooltip(bool open)
    {
        image.sprite = open ? openSprite : closedSprite;
        tooltip.Text = open ? openTooltip : closedTooltip;
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
    }
}