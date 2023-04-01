using UnityEngine;

public class JumpSettingsPanel : MonoBehaviour
{
    [SerializeField] private ValuePicker NJSPicker;
    [SerializeField] private ValuePicker JumpDistancePicker;
    [SerializeField] private ValuePicker ReactionTimePicker;


    public void SetNJS(float NJS)
    {
        BeatmapManager.NJS = NJS;
        UpdateValues();
    }


    public void SetJumpDistance(float distance)
    {
        BeatmapManager.JumpDistance = distance;
        UpdateValues();
    }


    public void SetReactionTime(float reactionTime)
    {
        //Picker uses ms, but BeatmapManager uses seconds
        reactionTime /= 1000;
        BeatmapManager.JumpDistance = reactionTime * BeatmapManager.NJS * 2;
        UpdateValues();
    }


    public void ResetValues()
    {
        Difficulty currentDifficulty = BeatmapManager.CurrentDifficulty;

        BeatmapManager.NJS = currentDifficulty.NoteJumpSpeed;
        BeatmapManager.JumpDistance = BeatmapManager.GetJumpDistance(BeatmapManager.HJD, BeatmapManager.Info._beatsPerMinute, BeatmapManager.NJS);

        UpdateValues();
    }


    public void UpdateValues()
    {
        NJSPicker.SetValueWithoutNotify(BeatmapManager.NJS);
        JumpDistancePicker.SetValueWithoutNotify(BeatmapManager.JumpDistance);
        ReactionTimePicker.SetValueWithoutNotify(BeatmapManager.ReactionTime * 1000);
        
        UpdateMap();
    }


    public void UpdateDifficulty(Difficulty newDifficulty)
    {
        UpdateValues();
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
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateDifficulty;

        NJSPicker.SetValueWithoutNotify(BeatmapManager.NJS);
        JumpDistancePicker.SetValueWithoutNotify(BeatmapManager.JumpDistance);
        ReactionTimePicker.SetValueWithoutNotify(BeatmapManager.ReactionTime * 1000);
    }


    private void OnDisable()
    {
        BeatmapManager.OnBeatmapDifficultyChanged -= UpdateDifficulty;
    }
}