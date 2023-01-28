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

    private RectTransform rectTransform;
    private Difficulty currentDifficulty = Difficulty.Empty;
    private DifficultyCharacteristic currentCharacteristic = DifficultyCharacteristic.Standard;
    private List<Difficulty> availableDifficulties = new List<Difficulty>();
    private int selectedCharacteristicIndex;


    public void UpdateCharacteristicButtons(DifficultyCharacteristic selectedCharacteristic)
    {
        //Since the selected characteristic always goes on bottom,
        //we need to figure out if it's selected or not first and treat the height accordingly
        float firstButtonHeight = selectedCharacteristic == currentDifficulty.characteristic ? buttonHeight : unselectedButtonHeight;

        float currentY = firstButtonHeight;
        int buttonIndex = 1;
        foreach(CharacteristicButton button in characteristicButtons)
        {
            if(!BeatmapManager.HasCharacteristic(button.characteristic))
            {
                //This characteristic isn't used
                button.gameObject.SetActive(false);
                continue;
            }

            bool isSelected = button.characteristic == selectedCharacteristic;
            bool isCurrent = button.characteristic == currentDifficulty.characteristic;

            float height = isSelected ? buttonHeight : unselectedButtonHeight;
            float width = isSelected ? selectedCharacteristicWidth : characteristicWidth;
            button.SetHeight(height);
            button.rectTransform.sizeDelta = new Vector2(width, height);

            //The current map characteristic should always be on the bottom
            float position = isCurrent ? 0 : currentY;
            button.rectTransform.anchoredPosition = new Vector2(button.rectTransform.anchoredPosition.x, position);

            if(isSelected)
            {
                selectedCharacteristicIndex = isCurrent ? 0 : buttonIndex;
            }

            if(!isCurrent)
            {
                //Brings the currentY to the top of the button
                currentY += height;
                buttonIndex++;
            }

            button.gameObject.SetActive(true);
        }
        //Set the height of this transform for raycast stuff
        float newHeight = Mathf.Max(currentY, rectTransform.sizeDelta.y);
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, newHeight);

        currentCharacteristic = selectedCharacteristic;
        availableDifficulties = BeatmapManager.GetDifficultiesByCharacteristic(currentCharacteristic);

        //Figure out which difficulty should be selected
        //This should be the one that shares the same effective index with the characteristic
        if(currentCharacteristic != currentDifficulty.characteristic)
        {
            int diffStartIndex = GetDifficultyStartIndex();
            int selectedDiffIndex = selectedCharacteristicIndex - diffStartIndex;
            UpdateDifficultyButtons(availableDifficulties[selectedDiffIndex].difficultyRank);
        }
        else
        {
            //The selected characteristic is the current one,
            //so the current difficulty should be selected as well by default
            UpdateDifficultyButtons(currentDifficulty.difficultyRank);
        }
    }


    public void UpdateDifficultyButtons(Diff selectedDifficulty)
    {
        bool sameCharacteristic = currentCharacteristic == currentDifficulty.characteristic;

        float currentY = GetDifficultyStartIndex() * unselectedButtonHeight;
        if(sameCharacteristic)
        {
            //The current difficulty is highlighted, and should be kept at the bottom
            bool selectedCurrent = selectedDifficulty == currentDifficulty.difficultyRank;
            float firstButtonHeight = selectedCurrent ? buttonHeight : unselectedButtonHeight;
            currentY += firstButtonHeight;
        }

        foreach(DifficultyButton button in difficultyButtons)
        {
            if(!availableDifficulties.Any(x => x.difficultyRank == button.difficulty))
            {
                //This difficulty isn't used
                button.gameObject.SetActive(false);
                continue;
            }

            bool isSelected = button.difficulty == selectedDifficulty;
            bool isCurrent = sameCharacteristic && button.difficulty == currentDifficulty.difficultyRank;

            float height = isSelected ? buttonHeight : unselectedButtonHeight;
            float width = isSelected ? selectedDifficultyWidth : difficultyWidth;
            button.rectTransform.sizeDelta = new Vector2(width, height);

            float position = isCurrent ? 0 : currentY;
            button.rectTransform.anchoredPosition = new Vector2(button.rectTransform.anchoredPosition.x, position);

            if(!isCurrent)
            {
                currentY += height;
                button.button.interactable = true;
            }
            else
            {
                //Make the current diff uninteractable to avoid reloading the same diff
                button.button.interactable = false;
            }

            button.gameObject.SetActive(true);
        }
        float newHeight = Mathf.Max(currentY, rectTransform.sizeDelta.y);
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, newHeight);
    }


    private int GetDifficultyStartIndex()
    {
        int diffCount = availableDifficulties.Count;
        return Mathf.Max(selectedCharacteristicIndex - (diffCount - 1), 0);
    }


    public void CollapseButtons()
    {
        foreach(CharacteristicButton button in characteristicButtons)
        {
            if(button.characteristic != currentDifficulty.characteristic)
            {
                //This isn't the button we're looking for
                button.gameObject.SetActive(false);
                continue;
            }

            //Just in case
            button.gameObject.SetActive(true);

            //Reset the height and position of this button
            button.SetHeight(unselectedButtonHeight);
            button.rectTransform.sizeDelta = new Vector2(characteristicWidth, unselectedButtonHeight);
            button.rectTransform.anchoredPosition = Vector2.zero;
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

            button.rectTransform.sizeDelta = new Vector2(difficultyWidth, unselectedButtonHeight);
            button.rectTransform.anchoredPosition = Vector2.zero;
        }

        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, buttonHeight);
    }


    private void SetDifficulty(Difficulty newDiff)
    {
        BeatmapManager.CurrentMap = newDiff;
    }


    public void ChangeDifficulty(Diff newDiff)
    {
        List<Difficulty> diffs = BeatmapManager.GetDifficultiesByCharacteristic(currentCharacteristic);
        try
        {
            Difficulty newDifficulty = diffs.Single(x => x.difficultyRank == newDiff);
            SetDifficulty(newDifficulty);
        }
        catch(InvalidOperationException)
        {
            Debug.LogWarning("Trying to load a difficulty that doesn't exist!");
        }
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


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }


    private void Start()
    {
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateDifficulty;

        CollapseButtons();
    }
}