using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DifficultyButton : MonoBehaviour, IPointerEnterHandler
{
    public Diff difficulty;
    public RectTransform rectTransform;
    public Button button;

    [SerializeField] private DifficultyButtonController controller;


    public void OnPointerEnter(PointerEventData eventData)
    {
        controller.UpdateDifficultyButtons(difficulty);
    }


    public void SetDifficulty()
    {
        controller.ChangeDifficulty(difficulty);
    }


    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        button = GetComponent<Button>();
    }
}