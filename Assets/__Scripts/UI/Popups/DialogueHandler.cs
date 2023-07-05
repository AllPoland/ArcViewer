using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueHandler : MonoBehaviour
{
    public static DialogueHandler Instance { get; private set; }

    public static List<DialogueBox> OpenBoxes = new List<DialogueBox>();
    public static bool LogActive => Instance.logCanvas.activeInHierarchy;
    public static bool DialogueActive => OpenBoxes.Count > 0 || Instance.infoPanel.activeInHierarchy || Instance.staticLightsWarningPanel.activeInHierarchy || LogActive;
    public static bool PopupActive => DialogueActive || Instance.sharePanel.activeInHierarchy || Instance.jumpSettingsPanel.activeInHierarchy || Instance.statsPanel.activeInHierarchy;

    [SerializeField] private GameObject dialogueBoxPrefab;

    public LogDisplay logDisplay;
    public GameObject logCanvas;
    public GameObject infoPanel;
    public GameObject sharePanel;
    public GameObject jumpSettingsPanel;
    public GameObject statsPanel;
    public GameObject staticLightsWarningPanel;


    public static void ShowDialogueBox(DialogueBoxType type, string text, Action<DialogueResponse> callback = null)
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


    public void SetStatsPanelActive(bool active)
    {
        statsPanel.SetActive(active);
    }


    public void ShowLog(Log log)
    {
        TimeManager.SetPlaying(false);
        logCanvas.SetActive(true);
        logDisplay.SetLog(log);
    }


    public static void ClearDialogueBoxes()
    {
        for(int i = OpenBoxes.Count - 1; i >= 0; i--)
        {
            OpenBoxes[i].Close();
        }

        ClearExtraPopups();
    }


    public static void ClearExtraPopups()
    {
        Instance.SetInfoPanelActive(false);
        Instance.SetSharePanelActive(false);
        Instance.SetJumpSettingsPanelActive(false);
        Instance.SetStatsPanelActive(false);
    }


    private void DialogueKeybinds()
    {
        if(staticLightsWarningPanel.activeInHierarchy)
        {
            //Don't use keybinds to exit the warning panel (it's important)
            return;
        }

        bool cancel = Input.GetButtonDown("Cancel");
        bool submit = Input.GetButtonDown("Submit");
        if(!cancel && !submit)
        {
            return;
        }

        if(LogActive)
        {
            if(cancel)
            {
                logCanvas.gameObject.SetActive(false);
            }
            return;
        }

        if(OpenBoxes.Count <= 0)
        {
            //No generic dialogue boxes, check for special dialogues
            if(cancel)
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

                if(statsPanel.activeInHierarchy)
                {
                    SetStatsPanelActive(false);
                    return;
                }
            }
            return;
        }

        DialogueBox currentBox = OpenBoxes[OpenBoxes.Count - 1];

        if(cancel)
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

        if(submit)
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