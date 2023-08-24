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

    private bool freeCam => CameraUpdater.FreeCam;


    private void UpdateButton()
    {
        buttonImage.color = freeCam ? freecamOnColor : freecamOffColor;
        tooltip.Text = freeCam ? freecamOnTooltip : freecamOffTooltip;
    }


    public void ToggleFreecam(bool updateTooltip)
    {
        UpdateButton();
    }


    private void OnEnable()
    {
        
    }


    private void OnDisable()
    {
        
    }
}