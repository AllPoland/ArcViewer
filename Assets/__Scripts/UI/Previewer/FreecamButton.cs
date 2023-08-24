using UnityEngine;
using UnityEngine.UI;

public class FreecamButton : MonoBehaviour
{
    [SerializeField] private Image buttonImage;
    [SerializeField] private Tooltip tooltip;

    [Space]
    [SerializeField] private Color freecamOffColor;
    [SerializeField] private Color freecamOnColor;

    [Space]
    [SerializeField] private string freecamOffTooltip;
    [SerializeField] private string freecamOnTooltip;

    private bool freecam => CameraUpdater.Freecam;


    private void UpdateButton()
    {
        buttonImage.color = freecam ? freecamOnColor : freecamOffColor;
        tooltip.Text = freecam ? freecamOnTooltip : freecamOffTooltip;
        tooltip.ForceUpdate();
    }


    public void ToggleFreecam()
    {
        CameraUpdater.Freecam = !freecam;
    }


    private void OnEnable()
    {
        CameraUpdater.OnFreecamUpdated += UpdateButton;
        UpdateButton();
    }


    private void OnDisable()
    {
        CameraUpdater.OnFreecamUpdated -= UpdateButton;
    }
}