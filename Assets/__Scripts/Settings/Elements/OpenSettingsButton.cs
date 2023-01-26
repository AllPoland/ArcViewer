using UnityEngine;
using UnityEngine.UI;

public class OpenSettingsButton : MonoBehaviour
{
    private Button button;


    private void Update()
    {
        if(Input.GetButtonDown("Toggle Options") && button.interactable && !DialogueHandler.DialogueActive)
        {
            button.onClick?.Invoke();
        }
    }


    private void OnEnable()
    {
        button = GetComponent<Button>();
    }
}