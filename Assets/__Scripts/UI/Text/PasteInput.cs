using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PasteInput : MonoBehaviour
{
    //Attempt at working around WebGL build's inability to paste.
    //This doesn't work because WebGL doesn't get access to the clipboard anyway
    //I'll need to find another way to make this work or just get a package for it

    [SerializeField] private TMP_InputField inputField;

    void Update()
    {
        if(inputField.isFocused && Input.GetButton("Control") && Input.GetButtonDown("Paste"))
        {
            Debug.Log($"Pasted '{GUIUtility.systemCopyBuffer}' into the Input Field.");
            inputField.text = GUIUtility.systemCopyBuffer;
        }
    }
}