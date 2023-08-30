using UnityEngine;
using TMPro;

public class FpsDisplay : MonoBehaviour
{
    public const float FramerateSampleTime = 0.5f;

    [SerializeField] private TextMeshProUGUI fpsCounter;
    [SerializeField] private TextMeshProUGUI replayFpsCounter;

    private bool showFPS;
    private bool showReplayFPS => showFPS && ReplayManager.IsReplayMode && UIStateManager.CurrentState == UIState.Previewer;

    private float currentFramerate => 1f / Time.deltaTime;

    private int checkedFrameCount;
    private float timeSinceFramerateUpdate;


    private void SetCountersActive()
    {
        fpsCounter.gameObject.SetActive(showFPS);
        replayFpsCounter.gameObject.SetActive(showReplayFPS);

        checkedFrameCount = 0;
        timeSinceFramerateUpdate = 0f;
    }


    private void UpdateReplayMode(bool replayMode) => SetCountersActive();


    private void UpdateSettings(string setting)
    {
        if(setting == "all" || setting == "fpscounter")
        {
            showFPS = SettingsManager.GetBool("fpscounter");
            SetCountersActive();
        }
    }


    private void UpdateBeat(float beat)
    {
        if(showReplayFPS)
        {
            replayFpsCounter.text = "Replay: " + PlayerPositionManager.AverageFPS;
        }
    }


    private void Update()
    {
        if(showFPS)
        {
            timeSinceFramerateUpdate += Time.deltaTime;
            checkedFrameCount++;

            if(timeSinceFramerateUpdate >= FramerateSampleTime)
            {
                float averageFramerate = 1f / (timeSinceFramerateUpdate / checkedFrameCount);
                int fps = Mathf.RoundToInt(averageFramerate);
                fpsCounter.text = "FPS: " + fps;

                checkedFrameCount = 0;
                timeSinceFramerateUpdate = 0f;
            }
        }
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;
        ReplayManager.OnReplayModeChanged += UpdateReplayMode;
        TimeManager.OnBeatChanged += UpdateBeat;

        if(SettingsManager.Loaded)
        {
            UpdateSettings("all");
        }
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
        ReplayManager.OnReplayModeChanged -= UpdateReplayMode;
        TimeManager.OnBeatChanged -= UpdateBeat;
    }
}