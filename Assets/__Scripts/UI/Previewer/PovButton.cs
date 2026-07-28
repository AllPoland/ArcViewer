using UnityEngine;

public class PovButton : MonoBehaviour
{
    [SerializeField] private QuickSettingButton button;


    private void OnEnable()
    {
        button.gameObject.SetActive(ReplayManager.IsReplayMode);
    }


    private void Update()
    {
        if(ReplayManager.IsReplayMode && !EventSystemHelper.SelectedObject && Input.GetButtonDown("TogglePerspective"))
        {
            button.ToggleSetting(false);
        }
    }
}