using UnityEngine;

public class JumpSettingsPanel : MonoBehaviour
{
    [SerializeField] private ValuePicker NJSPicker;
    [SerializeField] private ValuePicker JumpDistancePicker;


    public void SetNJS(float NJS)
    {
        BeatmapManager.NJS = NJS;
        UpdateMap();
    }


    public void SetJumpDistance(float distance)
    {
        BeatmapManager.JumpDistance = distance;
        UpdateMap();
    }


    public void ResetValues()
    {
        Difficulty currentDifficulty = BeatmapManager.CurrentMap;

        BeatmapManager.NJS = currentDifficulty.NoteJumpSpeed;
        BeatmapManager.JumpDistance = BeatmapManager.GetJumpDistance(BeatmapManager.HJD, BeatmapManager.Info._beatsPerMinute, BeatmapManager.NJS);

        UpdateValues(currentDifficulty);
    }


    public void UpdateValues(Difficulty newDifficulty)
    {
        NJSPicker.SetValueWithoutNotify(newDifficulty.NoteJumpSpeed);
        JumpDistancePicker.SetValueWithoutNotify(BeatmapManager.JumpDistance);
        
        UpdateMap();
    }


    public void UpdateMap()
    {
        if(!ObjectManager.Instance)
        {
            return;
        }

        //Force update the visuals when jump settings are changed
        ObjectManager.Instance.arcManager.UpdateMaterials();
        ObjectManager.Instance.wallManager.ClearRenderedWalls();
        ObjectManager.Instance.UpdateBeat(TimeManager.CurrentBeat);
    }


    private void OnEnable()
    {
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateValues;

        NJSPicker.SetValueWithoutNotify(BeatmapManager.NJS);
        JumpDistancePicker.SetValueWithoutNotify(BeatmapManager.JumpDistance);
    }


    private void OnDisable()
    {
        BeatmapManager.OnBeatmapDifficultyChanged -= UpdateValues;
    }
}