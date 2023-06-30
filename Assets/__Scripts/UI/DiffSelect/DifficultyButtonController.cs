using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class DifficultyButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float buttonHeight;
    [SerializeField] private float unselectedButtonHeight;

    [SerializeField] private float characteristicWidth;
    [SerializeField] private float selectedCharacteristicWidth;

    [SerializeField] private float difficultyWidth;
    [SerializeField] private float selectedDifficultyWidth;

    [SerializeField] private List<CharacteristicButton> characteristicButtons;

    [SerializeField] private List<DifficultyButton> difficultyButtons;

    private Difficulty currentDifficulty = Difficulty.Empty;
    private DifficultyCharacteristic currentCharacteristic = DifficultyCharacteristic.Standard;
    public List<Difficulty> availableDifficulties = new List<Difficulty>();
    private DifficultyRank selectedDifficulty;
    private int selectedCharacteristicIndex;


    public void UpdateCharacteristicButtons(DifficultyCharacteristic selectedCharacteristic)
    {
        //Since the selected characteristic always goes on bottom,
        //we need to figure out if it's selected or not first and treat the height accordingly
        float firstButtonHeight = selectedCharacteristic == currentDifficulty.characteristic ? buttonHeight : unselectedButtonHeight;

        for(int i = 0; i < characteristicButtons.Count; i++)
        {
            CharacteristicButton button = characteristicButtons[i];

            if(!BeatmapManager.HasCharacteristic(button.characteristic))
            {
                //This characteristic isn't used
                button.gameObject.SetActive(false);
                continue;
            }
            button.gameObject.SetActive(true);

            bool isSelected = button.characteristic == selectedCharacteristic;
            bool isCurrent = button.characteristic == currentDifficulty.characteristic;

            float height = isSelected ? buttonHeight : unselectedButtonHeight;
            float width = isSelected ? selectedCharacteristicWidth : characteristicWidth;
            button.SetHeight(height);
            button.rectTransform.sizeDelta = new Vector2(width, height);

            if(isSelected)
            {
                selectedCharacteristicIndex = i;
            }
        }

        currentCharacteristic = selectedCharacteristic;
        availableDifficulties = BeatmapManager.GetDifficultiesByCharacteristic(currentCharacteristic);

        //Figure out the offset difficulty buttons should be placed on
        //to line them up with the selected characteristic
        int diffStartIndex = GetDifficultyStartIndex();

        //The button directly next to this characteristic should be selected
        int selectedDiffIndex = selectedCharacteristicIndex - diffStartIndex;
        UpdateDifficultyButtons(availableDifficulties[selectedDiffIndex].difficultyRank);
    }


    public void UpdateDifficultyButtons(DifficultyRank newDifficulty)
    {
        selectedDifficulty = newDifficulty;

        bool sameCharacteristic = currentCharacteristic == currentDifficulty.characteristic;
        float currentY = GetDifficultyStartIndex() * unselectedButtonHeight;

        foreach(DifficultyButton button in difficultyButtons)
        {
            if(!availableDifficulties.Any(x => x.difficultyRank == button.difficulty))
            {
                //This difficulty isn't used
                button.gameObject.SetActive(false);
                continue;
            }
            button.gameObject.SetActive(true);

            bool isSelected = button.difficulty == selectedDifficulty;

            float height = isSelected ? buttonHeight : unselectedButtonHeight;
            float width = isSelected ? selectedDifficultyWidth : difficultyWidth;
            button.rectTransform.sizeDelta = new Vector2(width, height);

            button.rectTransform.anchoredPosition = new Vector2(0f, currentY);
            button.UpdateDiffLabel(availableDifficulties);

            currentY += height;
        }
    }


    private int GetDifficultyStartIndex()
    {
        int diffCount = availableDifficulties.Count;
        return Mathf.Max(selectedCharacteristicIndex - (diffCount - 1), 0);
    }


    public void CollapseButtons()
    {
        currentCharacteristic = currentDifficulty.characteristic;
        availableDifficulties = BeatmapManager.GetDifficultiesByCharacteristic(currentCharacteristic);

        foreach(CharacteristicButton button in characteristicButtons)
        {
            if(button.characteristic != currentDifficulty.characteristic)
            {
                //This isn't the button we're looking for
                button.gameObject.SetActive(false);
                continue;
            }

            button.gameObject.SetActive(true);

            //Reset the height and position of this button
            button.SetHeight(unselectedButtonHeight);
            button.rectTransform.sizeDelta = new Vector2(characteristicWidth, unselectedButtonHeight);
        }

        foreach(DifficultyButton button in difficultyButtons)
        {
            if(button.difficulty != currentDifficulty.difficultyRank)
            {
                //This isn't the button we're looking for
                button.gameObject.SetActive(false);
                continue;
            }

            button.gameObject.SetActive(true);

            button.UpdateDiffLabel(availableDifficulties);
            button.rectTransform.sizeDelta = new Vector2(difficultyWidth, unselectedButtonHeight);
            button.rectTransform.anchoredPosition = Vector2.zero;
        }
    }


    public void ChangeDifficulty(DifficultyRank newDiff)
    {
        List<Difficulty> diffs = BeatmapManager.GetDifficultiesByCharacteristic(currentCharacteristic);
        try
        {
            Difficulty newDifficulty = diffs.Single(x => x.difficultyRank == newDiff);

            //Don't bother updating the difficulty if it's the same one
            if(BeatmapManager.CurrentDifficulty != newDifficulty)
            {
                BeatmapManager.CurrentDifficulty = newDifficulty;
            }
        }
        catch(InvalidOperationException)
        {
            Debug.LogWarning("Trying to load a difficulty that doesn't exist!");
        }
    }


    public void ChangeCharacteristic()
    {
        if(currentCharacteristic == currentDifficulty.characteristic)
        {
            //We're already on this characteristic
            return;
        }

        ChangeDifficulty(selectedDifficulty);
    }


    public void UpdateDifficulty(Difficulty newDiff)
    {
        currentDifficulty = newDiff;
        CollapseButtons();
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        UpdateCharacteristicButtons(currentDifficulty.characteristic);
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        CollapseButtons();
    }


    private void OnEnable()
    {
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateDifficulty;

        CollapseButtons();
    }
}