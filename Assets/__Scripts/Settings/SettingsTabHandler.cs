using UnityEngine;

public class SettingsTabHandler : MonoBehaviour
{
    [SerializeField] private RectTransform contentTransform;


    public void ResetScroll()
    {
        contentTransform.anchoredPosition = Vector2.zero;
    }
}