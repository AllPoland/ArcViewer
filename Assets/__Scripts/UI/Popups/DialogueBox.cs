using System;
using UnityEngine;
using TMPro;

public class DialogueBox : MonoBehaviour
{
    public Action<DialogueResponse> Callback;
    public DialogueBoxType boxType { get; private set; }

    [SerializeField] private GameObject okButton;
    [SerializeField] private GameObject yesButton;
    [SerializeField] private GameObject noButton;
    [SerializeField] private GameObject cancelButton;
    [SerializeField] private TextMeshProUGUI dialogueText;


    public void SetText(string text)
    {
        dialogueText.text = text;
    }


    public void SetType(DialogueBoxType type)
    {
        boxType = type;

        okButton.SetActive(type == DialogueBoxType.Ok);
        yesButton.SetActive(type == DialogueBoxType.YesNo || type == DialogueBoxType.YesNoCancel);
        noButton.SetActive(type == DialogueBoxType.YesNo || type == DialogueBoxType.YesNoCancel);
        cancelButton.SetActive(type == DialogueBoxType.YesNoCancel);
    }


    public void Close()
    {
        DialogueHandler.OpenBoxes.Remove(this);
        Destroy(gameObject);
    }


    public void ExecuteCallback(int response)
    {
        if(Callback != null)
        {
            Callback((DialogueResponse)response);
        }

        Close();
    }
}