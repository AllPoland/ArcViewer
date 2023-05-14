using UnityEngine;

public class ResetButton : MonoBehaviour
{
    public void ShowDialogue()
    {
        DialogueHandler.ShowDialogueBox(DialogueBoxType.YesNo, "Are you sure you want to reset all settings?\nThis can't be undone!", ResetSettings);
    }


    public void ResetSettings(DialogueResponse response)
    {
        if(response == DialogueResponse.Yes)
        {
            SettingsManager.SetDefaults();
        }
    }
}