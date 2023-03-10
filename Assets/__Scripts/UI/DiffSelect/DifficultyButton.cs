using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class DifficultyButton : MonoBehaviour, IPointerEnterHandler
{
    public DifficultyRank difficulty;
    public RectTransform rectTransform;
    public Button button;

    [SerializeField] private DifficultyButtonController controller;
    [SerializeField] private TextMeshProUGUI diffLabel;


    public void OnPointerEnter(PointerEventData eventData)
    {
        controller.UpdateDifficultyButtons(difficulty);
    }


    public void UpdateDiffLabel(List<Difficulty> availableDifficulties)
    {
        Difficulty thisDifficulty = availableDifficulties.FirstOrDefault(x => x.difficultyRank == difficulty);
        if(thisDifficulty != null && thisDifficulty.Label != "")
        {
            diffLabel.text = thisDifficulty.Label;
        }
        else diffLabel.text = Difficulty.DiffLabelFromRank(difficulty);
    }


    public void SetDifficulty()
    {
        controller.ChangeDifficulty(difficulty);
    }


    private void OnEnable()
    {
        if(!rectTransform)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        if(!button)
        {
            button = GetComponent<Button>();
        }
    }
}