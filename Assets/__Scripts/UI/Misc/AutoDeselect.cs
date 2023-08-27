using UnityEngine;

public class AutoDeselect : MonoBehaviour
{
    private void OnDisable()
    {
        EventSystemHelper.Deselect();
    }
}