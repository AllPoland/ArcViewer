using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueHandler : MonoBehaviour
{
    public static DialogueHandler Instance { get; private set; }

    public static List<DialogueBox> OpenBoxes = new List<DialogueBox>();
    public static bool DialogueActive => OpenBoxes.Count > 0 || Instance.infoPanel.activeInHierarchy;
    public static bool PopupActive => DialogueActive || Instance.sharePanel.activeInHierarchy || Instance.jumpSettingsPanel.activeInHierarchy;

    public GameObject infoPanel;
    public GameObject sharePanel;
    public GameObject jumpSettingsPanel;

    [SerializeField] private GameObject dialogueBoxPrefab;


    public static void ShowDialogueBox(string text, Action<DialogueResponse> callback, DialogueBoxType type)
    {
        GameObject dialogueBoxObject = Instantiate(Instance.dialogueBoxPrefab);
        dialogueBoxObject.transform.SetParent(Instance.transform, false);

        DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();

        dialogueBox.Callback = callback;
        dialogueBox.SetText(text);
        dialogueBox.SetType(type);

        OpenBoxes.Add(dialogueBox);
    }


    public void SetInfoPanelActive(bool active)
    {
        infoPanel.SetActive(active);
    }


    public void SetSharePanelActive(bool active)
    {
        sharePanel.SetActive(active);
    }


    public void SetJumpSettingsPanelActive(bool active)
    {
        jumpSettingsPanel.SetActive(active);
    }


    public static void ClearDialogueBoxes()
    {
        for(int i = OpenBoxes.Count - 1; i >= 0; i--)
        {
            OpenBoxes[i].Close();
        }

        Instance.SetInfoPanelActive(false);
        Instance.SetSharePanelActive(false);
    }


    private void DialogueKeybinds()
    {
        if(OpenBoxes.Count <= 0)
        {
            //No generic dialogue boxes, check for special dialogues
            if(Input.GetButtonDown("Cancel"))
            {
                if(infoPanel.activeInHierarchy)
                {
                    SetInfoPanelActive(false);
                    return;
                }

                if(sharePanel.activeInHierarchy)
                {
                    SetSharePanelActive(false);
                    return;
                }

                if(jumpSettingsPanel.activeInHierarchy)
                {
                    SetJumpSettingsPanelActive(false);
                    return;
                }
            }
            return;
        }

        DialogueBox currentBox = OpenBoxes[OpenBoxes.Count - 1];

        if(Input.GetButtonDown("Cancel"))
        {
            switch(currentBox.boxType)
            {
                case DialogueBoxType.Ok:
                    currentBox.ExecuteCallback(3);
                    break;
                case DialogueBoxType.YesNo:
                    currentBox.ExecuteCallback(2);
                    break;
                case DialogueBoxType.YesNoCancel:
                    currentBox.ExecuteCallback(0);
                    break;
            }
        }

        if(Input.GetButtonDown("Submit"))
        {
            switch(currentBox.boxType)
            {
                case DialogueBoxType.Ok:
                    currentBox.ExecuteCallback(3);
                    break;
                case DialogueBoxType.YesNo:
                    currentBox.ExecuteCallback(1);
                    break;
                case DialogueBoxType.YesNoCancel:
                    currentBox.ExecuteCallback(1);
                    break;
            }
        }
    }


    private void Update()
    {
        if(PopupActive)
        {
            DialogueKeybinds();
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


public enum DialogueResponse
{
    Cancel,
    Yes,
    No,
    Ok
}