using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueHandler : MonoBehaviour
{
    public static DialogueHandler Instance { get; private set; }

    public static List<DialogueBox> OpenBoxes = new List<DialogueBox>();
    public static bool DialogueActive => OpenBoxes.Count > 0;

    [SerializeField] private GameObject dialogueBoxPrefab;


    public static void ShowDialogueBox(string text, Action<int> callback, DialogueBoxType type)
    {
        GameObject dialogueBoxObject = Instantiate(Instance.dialogueBoxPrefab);
        dialogueBoxObject.transform.SetParent(Instance.transform, false);

        DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();

        dialogueBox.Callback = callback;
        dialogueBox.SetText(text);
        dialogueBox.SetType(type);

        OpenBoxes.Add(dialogueBox);
    }


    public static void ClearDialogueBoxes()
    {
        for(int i = OpenBoxes.Count - 1; i >= 0; i--)
        {
            OpenBoxes[i].Close();
        }
    }


    private void OnEnable()
    {
        if(Instance != null && Instance != this)
        {
            this.enabled = false;
            return;
        }

        Instance = this;
    }
}


public enum DialogueBoxType
{
    Ok,
    YesNo,
    YesNoCancel
}