using UnityEngine;
using UnityEngine.EventSystems;

public class CharacteristicButton : MonoBehaviour, IPointerEnterHandler
{
    public DifficultyCharacteristic characteristic;
    public RectTransform rectTransform;

    [SerializeField] private DifficultyButtonController controller;
    [SerializeField] private RectTransform imageTransform;


    public void OnPointerEnter(PointerEventData eventData)
    {
        controller.UpdateCharacteristicButtons(characteristic);
    }


    public void SelectCharcteristic()
    {
        controller.ChangeCharacteristic();
    }


    public void SetHeight(float newHeight)
    {
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, newHeight);
        imageTransform.sizeDelta = new Vector2(newHeight, newHeight);
    }
}