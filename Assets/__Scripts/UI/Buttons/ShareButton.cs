using UnityEngine;
using UnityEngine.UI;

public class ShareButton : MonoBehaviour
{
    [SerializeField] private string enabledTooltip;
    [SerializeField] private string disabledTooltip;

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

        button.interactable = !string.IsNullOrEmpty(UrlArgHandler.LoadedMapID) || !string.IsNullOrEmpty(UrlArgHandler.LoadedMapURL);
        tooltip.Text = button.interactable ? enabledTooltip : disabledTooltip;
    }
}