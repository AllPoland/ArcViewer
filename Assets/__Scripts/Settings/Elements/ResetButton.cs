using UnityEngine;

public class ResetButton : MonoBehaviour
{
    public void ShowDialogue()
    {
        DialogueHandler.ShowDialogueBox("Are you sure you want to reset your settings?\nThis can't be undone!", ResetSettings, DialogueBoxType.YesNo);
    }


    public void ResetSettings(DialogueResponse response)
    {
        if(response == DialogueResponse.Yes)
        {
            SettingsManager.SetDefaults();
        }
    }
}