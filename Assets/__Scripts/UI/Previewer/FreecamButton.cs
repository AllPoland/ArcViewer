using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class FreecamButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button button;
    [SerializeField] private Tooltip tooltip;

    [Space]
    [SerializeField] private string freecamOffTooltip;
    [SerializeField] private string freecamOnTooltip;

    private bool freecam => CameraUpdater.Freecam;

    private bool hovered;


    private void UpdateButton()
    {
        button.interactable = freecam;
        tooltip.Text = freecam ? freecamOnTooltip : freecamOffTooltip;
        tooltip.ForceUpdate();
    }


    public void ToggleFreecam()
    {
        if(CameraUpdater.Freecam)
        {
            CameraUpdater.Freecam = false;
        }
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
    }


    private void Update()
    {
        if(hovered && !CameraUpdater.Freecam && Input.GetMouseButtonDown(1))
        {
            //Allows right click to still enable freecam when this button is hovered over
            CameraUpdater.Freecam = true;
        }
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