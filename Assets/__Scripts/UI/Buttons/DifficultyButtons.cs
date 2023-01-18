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

    private BeatmapManager beatmapManager;
    private DifficultyCharacteristic currentCharacteristic;


    public void UpdateButtons(Difficulty currentDifficulty)
    {
        DisableAllButtons();

        currentCharacteristic = currentDifficulty.characteristic;
        Debug.Log($"Current diff is {currentCharacteristic}, {currentDifficulty.difficultyRank}");
        float y = buttonY;

        if(beatmapManager.StandardDifficulties.Count > 0)
        {
            EnableButton(standardButton, ref y);
        }
        if(beatmapManager.OneSaberDifficulties.Count > 0)
        {
            EnableButton(oneSaberButton, ref y);
        }
        if(beatmapManager.NoArrowsDifficulties.Count > 0)
        {
            EnableButton(noArrowsButton, ref y);
        }
        if(beatmapManager.ThreeSixtyDifficulties.Count > 0)
        {
            EnableButton(threeSixtyButton, ref y);
        }
        if(beatmapManager.NinetyDifficulties.Count > 0)
        {
            EnableButton(ninetyButton, ref y);
        }
        if(beatmapManager.LightshowDifficulties.Count > 0)
        {
            EnableButton(lightshowButton, ref y);
        }
        if(beatmapManager.LawlessDifficulties.Count > 0)
        {
            EnableButton(lawlessButton, ref y);
        }
        if(beatmapManager.UnknownDifficulties.Count > 0)
        {
            EnableButton(unknownButton, ref y);
        }

        List<Difficulty> characteristicDiffs = beatmapManager.GetDifficultiesByCharacteristic(currentCharacteristic);
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
        List<Difficulty> newDiffs = beatmapManager.GetDifficultiesByCharacteristic((DifficultyCharacteristic)newCharacteristic);
        if(newDiffs.Count == 0)
        {
            Debug.LogWarning("Trying to load a characteristic the map doesn't have!");
            return;
        }

        Difficulty preferredDiff = newDiffs.Find(x => x.difficultyRank == beatmapManager.CurrentMap.difficultyRank);
        if(preferredDiff != null && !preferredDiff.Equals( new Difficulty() ))
        {
            beatmapManager.CurrentMap = preferredDiff;
        }
        else
        {
            Debug.Log("Unable to find a difficulty of matching rank. Using default.");
            beatmapManager.CurrentMap = newDiffs[newDiffs.Count - 1];
        }
    }


    public void ChangeDifficulty(int newDifficulty)
    {
        List<Difficulty> diffs = beatmapManager.GetDifficultiesByCharacteristic(beatmapManager.CurrentMap.characteristic);
        Difficulty newDiff = diffs.Find(x => x.difficultyRank == (Diff)newDifficulty);
        if(!newDiff.Equals( new Difficulty() ))
        {
            beatmapManager.CurrentMap = newDiff;
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


    private void Start()
    {
        beatmapManager = BeatmapManager.Instance;

        if(beatmapManager == null)
        {
            enabled = false;
            return;
        }

        beatmapManager.OnBeatmapDifficultyChanged += UpdateButtons;
    }
}