using UnityEngine;

public class BackButton : MonoBehaviour
{
    public bool justClosed;

    public void SetMapSelection(DialogueResponse response)
    {
        if(response == DialogueResponse.Yes)
        {
            UIStateManager.CurrentState = UIState.MapSelection;
        }
        else
        {
            justClosed = true;
        }
    }


    public void ShowDialogue()
    {
        DialogueHandler.ShowDialogueBox(DialogueBoxType.YesNo, "Are you sure you want to exit the map?", SetMapSelection);
    }


    private void Update()
    {
        if(DialogueHandler.PopupActive)
        {
            justClosed = true;
            return;
        }

        if(Input.GetButtonDown("Cancel") && UIStateManager.CurrentState == UIState.Previewer && !justClosed)
        {
            ShowDialogue();
        }
        else justClosed = false;
    }
}