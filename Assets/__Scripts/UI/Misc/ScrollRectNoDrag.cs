using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ScrollRectNoDrag : ScrollRect
{
    //Just a simple class to act as a scroll rect component with no click-and-drag function
    public override void OnBeginDrag(PointerEventData eventData) { }
    public override void OnDrag(PointerEventData eventData) { }
    public override void OnEndDrag(PointerEventData eventData) { }
}