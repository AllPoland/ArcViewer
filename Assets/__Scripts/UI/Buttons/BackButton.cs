using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackButton : MonoBehaviour
{
    public void SetMapSelection()
    {
        UIStateManager.CurrentState = UIState.MapSelection;
    }


    private void Update()
    {
        if(Input.GetButtonDown("Cancel") && UIStateManager.CurrentState == UIState.Previewer)
        {
            SetMapSelection();
        }
    }
}