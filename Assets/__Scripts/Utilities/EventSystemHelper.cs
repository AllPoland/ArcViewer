using UnityEngine;
using UnityEngine.EventSystems;

public static class EventSystemHelper
{
    private static EventSystem eventSystem => EventSystem.current;


    public static void SetSelectedGameObject(GameObject selected)
    {
        if(!eventSystem?.alreadySelecting ?? false)
        {
            eventSystem.SetSelectedGameObject(selected);
        }
    }


    public static void Deselect()
    {
        if(!eventSystem?.alreadySelecting ?? false)
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }


    public static GameObject SelectedObject => eventSystem.currentSelectedGameObject;
}