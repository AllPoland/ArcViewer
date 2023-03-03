using UnityEngine;

public class JumpSettingsPanel : MonoBehaviour
{
    [SerializeField] private ValuePicker NJSPicker;
    [SerializeField] private ValuePicker SpawnOffsetPicker;


    public void SetNJS(float NJS)
    {
        BeatmapManager.NJS = NJS;
        UpdateMap();
    }


    public void SetSpawnOffset(float offset)
    {
        BeatmapManager.SpawnOffset = offset;
        UpdateMap();
    }


    public void ResetValues()
    {
        Difficulty currentDifficulty = BeatmapManager.CurrentMap;

        BeatmapManager.NJS = currentDifficulty.NoteJumpSpeed;
        BeatmapManager.SpawnOffset = currentDifficulty.SpawnOffset;

        UpdateValues(currentDifficulty);
    }


    public void UpdateValues(Difficulty newDifficulty)
    {
        NJSPicker.SetValueWithoutNotify(newDifficulty.NoteJumpSpeed);
        SpawnOffsetPicker.SetValueWithoutNotify(newDifficulty.SpawnOffset);
        
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
        SpawnOffsetPicker.SetValueWithoutNotify(BeatmapManager.SpawnOffset);
    }


    private void OnDisable()
    {
        BeatmapManager.OnBeatmapDifficultyChanged -= UpdateValues;
    }
}