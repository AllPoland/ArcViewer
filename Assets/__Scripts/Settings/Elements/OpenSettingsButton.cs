using UnityEngine;
using UnityEngine.UI;

public class OpenSettingsButton : MonoBehaviour
{
    [SerializeField] private Button button;

    private SettingsMenu settingsMenu;


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
    }
}