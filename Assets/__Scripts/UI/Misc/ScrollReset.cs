using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ScrollReset : MonoBehaviour
{
    [SerializeField] Vector2 defaultPosition;

    private RectTransform rectTransform;


    private void OnEnable()
    {
        if(!rectTransform)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        rectTransform.anchoredPosition = defaultPosition;
    }
}