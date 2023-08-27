using UnityEngine;

public class PovButton : MonoBehaviour
{
    [SerializeField] private QuickSettingButton button;


    private void Update()
    {
        if(ReplayManager.IsReplayMode && Input.GetButtonDown("TogglePerspective"))
        {
            button.ToggleSetting(false);
        }
    }
}