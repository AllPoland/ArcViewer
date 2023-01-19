using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DifficultyButtons : MonoBehaviour
{
    [SerializeField] private float buttonY;
    [SerializeField] private float buttonHeight;

    [Header("Characteristic Buttons")]
    [SerializeField] private GameObject standardButton;
    [SerializeField] private GameObject oneSaberButton;
    [SerializeField] private GameObject noArrowsButton;
    [SerializeField] private GameObject threeSixtyButton;
    [SerializeField] private GameObject ninetyButton;
    [SerializeField] private GameObject lightshowButton;
    [SerializeField] private GameObject lawlessButton;
    [SerializeField] private GameObject unknownButton;

    [Header("Difficulty Buttons")]
    [SerializeField] private GameObject easyButton;
    [SerializeField] private GameObject normalButton;
    [SerializeField] private GameObject hardButton;
    [SerializeField] private GameObject expertButton;
    [SerializeField] private GameObject expertPlusButton;

    private DifficultyCharacteristic currentCharacteristic;


    public void UpdateButtons(Difficulty currentDifficulty)
    {
        DisableAllButtons();

        currentCharacteristic = currentDifficulty.characteristic;
        Debug.Log($"Current diff is {currentCharacteristic}, {currentDifficulty.difficultyRank}");
        float y = buttonY;

        if(BeatmapManager.StandardDifficulties.Count > 0)
        {
            EnableButton(standardButton, ref y);
        }
        if(BeatmapManager.OneSaberDifficulties.Count > 0)
        {
            EnableButton(oneSaberButton, ref y);
        }
        if(BeatmapManager.NoArrowsDifficulties.Count > 0)
        {
            EnableButton(noArrowsButton, ref y);
        }
        if(BeatmapManager.ThreeSixtyDifficulties.Count > 0)
        {
            EnableButton(threeSixtyButton, ref y);
        }
        if(BeatmapManager.NinetyDifficulties.Count > 0)
        {
            EnableButton(ninetyButton, ref y);
        }
        if(BeatmapManager.LightshowDifficulties.Count > 0)
        {
            EnableButton(lightshowButton, ref y);
        }
        if(BeatmapManager.LawlessDifficulties.Count > 0)
        {
            EnableButton(lawlessButton, ref y);
        }
        if(BeatmapManager.UnknownDifficulties.Count > 0)
        {
            EnableButton(unknownButton, ref y);
        }

        List<Difficulty> characteristicDiffs = BeatmapManager.GetDifficultiesByCharacteristic(currentCharacteristic);
        y = buttonY;

        if(characteristicDiffs.Any(x => x.difficultyRank == Diff.Easy))
        {
            EnableButton(easyButton, ref y);
        }
        if(characteristicDiffs.Any(x => x.difficultyRank == Diff.Normal))
        {
            EnableButton(normalButton, ref y);
        }
        if(characteristicDiffs.Any(x => x.difficultyRank == Diff.Hard))
        {
            EnableButton(hardButton, ref y);
        }
        if(characteristicDiffs.Any(x => x.difficultyRank == Diff.Expert))
        {
            EnableButton(expertButton, ref y);
        }
        if(characteristicDiffs.Any(x => x.difficultyRank == Diff.ExpertPlus))
        {
            EnableButton(expertPlusButton, ref y);
        }
    }


    public void DisableAllButtons()
    {
        standardButton.SetActive(false);
        oneSaberButton.SetActive(false);
        noArrowsButton.SetActive(false);
        threeSixtyButton.SetActive(false);
        ninetyButton.SetActive(false);
        lightshowButton.SetActive(false);
        lawlessButton.SetActive(false);
        unknownButton.SetActive(false);

        easyButton.SetActive(false);
        normalButton.SetActive(false);
        hardButton.SetActive(false);
        expertButton.SetActive(false);
        expertPlusButton.SetActive(false);
    }


    public void ChangeCharacteristic(int newCharacteristic)
    {
        List<Difficulty> newDiffs = BeatmapManager.GetDifficultiesByCharacteristic((DifficultyCharacteristic)newCharacteristic);
        if(newDiffs.Count == 0)
        {
            Debug.LogWarning("Trying to load a characteristic the map doesn't have!");
            return;
        }

        Difficulty preferredDiff = newDiffs.Find(x => x.difficultyRank == BeatmapManager.CurrentMap.difficultyRank);
        if(preferredDiff != null && !preferredDiff.Equals( new Difficulty() ))
        {
            BeatmapManager.CurrentMap = preferredDiff;
        }
        else
        {
            Debug.Log("Unable to find a difficulty of matching rank. Using default.");
            BeatmapManager.CurrentMap = newDiffs[newDiffs.Count - 1];
        }
    }


    public void ChangeDifficulty(int newDifficulty)
    {
        List<Difficulty> diffs = BeatmapManager.GetDifficultiesByCharacteristic(BeatmapManager.CurrentMap.characteristic);
        Difficulty newDiff = diffs.Find(x => x.difficultyRank == (Diff)newDifficulty);
        if(!newDiff.Equals( new Difficulty() ))
        {
            BeatmapManager.CurrentMap = newDiff;
        }
        else
        {
            Debug.LogWarning("Trying to load a difficulty the map doesn't have!");
        }
    }


    private void EnableButton(GameObject toEnable, ref float y)
    {
        toEnable.SetActive(true);
        toEnable.transform.localPosition = new Vector2(toEnable.transform.localPosition.x, y);
        y += buttonHeight;
    }


    private void OnEnable()
    {
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateButtons;
    }


    private void OnDisable()
    {
        BeatmapManager.OnBeatmapDifficultyChanged -= UpdateButtons;
    }
}