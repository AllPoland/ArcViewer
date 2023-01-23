using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OpenSettingsButton : MonoBehaviour
{
    private Button button;


    private void Update()
    {
        if(Input.GetButtonDown("Toggle Options") && button.interactable)
        {
            button.onClick?.Invoke();
        }
    }


    private void OnEnable()
    {
        button = GetComponent<Button>();
    }
}