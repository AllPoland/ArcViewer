using UnityEngine;
using UnityEngine.UI;

public class ShareButton : MonoBehaviour
{
    [SerializeField] private string enabledTooltip;
    [SerializeField] private string disabledTooltip;
    [SerializeField] private string disabledReplayTooltip;

    private Button button;
    private Tooltip tooltip;


    private void OnEnable()
    {
        if(!button)
        {
            button = GetComponent<Button>();
        }
        if(!tooltip)
        {
            tooltip = GetComponent<Tooltip>();
        }

        if(ReplayManager.IsReplayMode)
        {
            button.interactable = !string.IsNullOrEmpty(UrlArgHandler.LoadedReplayID) || !string.IsNullOrEmpty(UrlArgHandler.LoadedReplayURL);
            tooltip.Text = button.interactable ? enabledTooltip : disabledReplayTooltip;
        }
        else
        {
            button.interactable = !string.IsNullOrEmpty(UrlArgHandler.LoadedMapID) || !string.IsNullOrEmpty(UrlArgHandler.LoadedMapURL);
            tooltip.Text = button.interactable ? enabledTooltip : disabledTooltip;
        }
    }
}