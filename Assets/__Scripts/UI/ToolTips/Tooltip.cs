using UnityEngine;
using UnityEngine.EventSystems;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string Text = "";


    public void ShowTooltip()
    {
        TooltipManager.AddTooltip(this);
    }


    public void HideTooltip()
    {
        TooltipManager.RemoveTooltip(this);
    }


    public void ForceUpdate()
    {
        if(TooltipManager.HasTooltip(this))
        {
            TooltipManager.RemoveTooltip(this);
            TooltipManager.AddTooltip(this);
        }
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }


    private void OnDisable()
    {
        HideTooltip();
    }
}