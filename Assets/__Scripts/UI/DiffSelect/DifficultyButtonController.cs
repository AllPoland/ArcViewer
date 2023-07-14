using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class DifficultyButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float buttonHeight;
    [SerializeField] private float unselectedButtonHeight;

    [Space]
    [SerializeField] private float characteristicWidth;
    [SerializeField] private float selectedCharacteristicWidth;

    [Space]
    [SerializeField] private float difficultyWidth;
    [SerializeField] private float selectedDifficultyWidth;

    [Space]
    [SerializeField] private List<CharacteristicButton> characteristicButtons;
    [SerializeField] private List<DifficultyButton> difficultyButtons;

    public List<Difficulty> availableDifficulties = new List<Difficulty>();

    private Difficulty currentDifficulty = Difficulty.Empty;
    private DifficultyCharacteristic currentCharacteristic = DifficultyCharacteristic.Standard;

    private DifficultyRank selectedDifficulty;
    private int currentCharacteristicIndex;


    public void UpdateCharacteristicButtons(DifficultyCharacteristic selectedCharacteristic)
    {
        //Start at 1 because the selected characteristic will always take 0
        int buttonIndex = 1;
        foreach(CharacteristicButton button in characteristicButtons)
        {
            if(!BeatmapManager.HasCharacteristic(button.characteristic))
            {
                //This characteristic isn't used
                button.gameObject.SetActive(false);
                continue;
            }
            bool isCurrent = button.characteristic == currentDifficulty.characteristic;
            if(ReplayManager.IsReplayMode && !isCurrent)
            {
                //Don't show other characteristics when in a replay
                button.gameObject.SetActive(false);
                continue;
            }

            button.gameObject.SetActive(true);

            bool isSelected = button.characteristic == selectedCharacteristic;

            float height = isSelected ? buttonHeight : unselectedButtonHeight;
            float width = isSelected ? selectedCharacteristicWidth : characteristicWidth;

            button.SetHeight(height);
            button.rectTransform.sizeDelta = new Vector2(width, height);

            if(isSelected)
            {
                currentCharacteristicIndex = isCurrent ? 0 : buttonIndex;
            }

            if(isCurrent)
            {
                //The current map characteristic should always be on the bottom
                button.transform.SetSiblingIndex(0);
            }
            else
            {
                button.transform.SetSiblingIndex(buttonIndex);
                buttonIndex++;
            }
        }

        currentCharacteristic = selectedCharacteristic;
        availableDifficulties = BeatmapManager.GetDifficultiesByCharacteristic(currentCharacteristic);

        //Figure out the offset difficulty buttons should be placed on
        //to line them up with the selected characteristic
        int diffStartIndex = GetDifficultyStartIndex();

        //The button directly next to this characteristic should be selected
        int selectedDiffIndex = currentCharacteristicIndex - diffStartIndex;
        UpdateDifficultyButtons(availableDifficulties[selectedDiffIndex].difficultyRank);
    }


    public void UpdateDifficultyButtons(DifficultyRank newDifficulty)
    {
        selectedDifficulty = newDifficulty;

        float currentY = GetDifficultyStartIndex() * unselectedButtonHeight;
        foreach(DifficultyButton button in difficultyButtons)
        {
            if(!availableDifficulties.Any(x => x.difficultyRank == button.difficulty))
            {
                //This difficulty isn't used in the characteristic
                button.gameObject.SetActive(false);
                continue;
            }
            if(ReplayManager.IsReplayMode)
            {
                if(button.difficulty != currentDifficulty.difficultyRank)
                {
                    //Don't show other difficulties when in a replay
                    button.gameObject.SetActive(false);
                    continue;
                }
                else
                {
                    //The current difficulty will always be selected in replays
                    button.gameObject.SetActive(true);
                    button.UpdateDiffLabel(availableDifficulties);
                    button.SetButtonSize(selectedDifficultyWidth, buttonHeight, true);
                    button.rectTransform.anchoredPosition = Vector2.zero;
                    break;
                }
            }
            button.gameObject.SetActive(true);
            button.UpdateDiffLabel(availableDifficulties);

            bool isSelected = button.difficulty == selectedDifficulty;

            float width = isSelected ? selectedDifficultyWidth : difficultyWidth;
            float height = isSelected ? buttonHeight : unselectedButtonHeight;

            button.SetButtonSize(width, height, isSelected);
            button.rectTransform.anchoredPosition = new Vector2(0f, currentY);

            currentY += height;
        }
    }


    private int GetDifficultyStartIndex()
    {
        int diffCount = availableDifficulties.Count;
        return Mathf.Max(currentCharacteristicIndex - (diffCount - 1), 0);
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
        if(ReplayManager.IsReplayMode)
        {
            //Replays need to stick to their difficulty
            return;
        }

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
        CollapseButtons();
    }


    public void ChangeCharacteristic()
    {
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