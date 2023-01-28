using UnityEngine;

public class ResetButton : MonoBehaviour
{
    public void ShowDialogue()
    {
        DialogueHandler.ShowDialogueBox("Are you sure you want to reset your settings?", ResetSettings, DialogueBoxType.YesNo);
    }


    public void ResetSettings(DialogueResponse response)
    {
        if(response == DialogueResponse.Yes)
        {
            SettingsManager.SetDefaults();
        }
    }
}