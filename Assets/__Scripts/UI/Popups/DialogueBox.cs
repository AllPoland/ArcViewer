using System;
using UnityEngine;
using TMPro;

public class DialogueBox : MonoBehaviour
{
    public Action<int> Callback;

    [SerializeField] private GameObject okButton;
    [SerializeField] private GameObject yesButton;
    [SerializeField] private GameObject noButton;
    [SerializeField] private GameObject cancelButton;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [SerializeField] private float buttonDistance;


    public void SetText(string text)
    {
        dialogueText.text = text;
    }


    private void SetButtonPosition(GameObject button, float x)
    {
        RectTransform buttonTransform = button.GetComponent<RectTransform>();
        buttonTransform.anchoredPosition = new Vector2(x, buttonTransform.anchoredPosition.y);
    }


    public void SetType(DialogueBoxType type)
    {
        int buttonCount = 0;
        switch(type)
        {
            case DialogueBoxType.Ok:
                okButton.SetActive(true);
                buttonCount = 1;
                break;
            case DialogueBoxType.YesNo:
                yesButton.SetActive(true);
                noButton.SetActive(true);
                buttonCount = 2;
                break;
            case DialogueBoxType.YesNoCancel:
                yesButton.SetActive(true);
                noButton.SetActive(true);
                cancelButton.SetActive(true);
                buttonCount = 3;
                break;
        }

        float totalButtonDistance = buttonDistance * (buttonCount - 1);
        float x = -(totalButtonDistance / 2);

        if(okButton.activeInHierarchy)
        {
            SetButtonPosition(okButton, x);
            x += buttonDistance;
        }
        if(yesButton.activeInHierarchy)
        {
            SetButtonPosition(yesButton, x);
            x += buttonDistance;
        }
        if(noButton.activeInHierarchy)
        {
            SetButtonPosition(noButton, x);
            x += buttonDistance;
        }
        if(cancelButton.activeInHierarchy)
        {
            SetButtonPosition(cancelButton, x);
            x += buttonDistance;
        }
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
            Callback(response);
        }

        Close();
    }
}