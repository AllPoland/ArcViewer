using System.Collections.Generic;
using UnityEngine;

public class PlayerPositionManager : MonoBehaviour
{
    public MapElementList<ReplayFrame> replayFrames = new MapElementList<ReplayFrame>();

    [SerializeField] private GameObject headVisual;
    [SerializeField] private GameObject leftSaberVisual;
    [SerializeField] private GameObject rightSaberVisual;

    [Space]
    [SerializeField] private Vector3 defaultHmdPosition;
    [SerializeField] private Vector3 defaultLeftSaberPosition;
    [SerializeField] private Vector3 defaultRightSaberPosition;


    private void SetDefaultPositions()
    {
        headVisual.transform.localPosition = defaultHmdPosition;
        headVisual.transform.localRotation = Quaternion.identity;

        leftSaberVisual.transform.localPosition = defaultLeftSaberPosition;
        leftSaberVisual.transform.localRotation = Quaternion.identity;

        rightSaberVisual.transform.localPosition = defaultRightSaberPosition;
        rightSaberVisual.transform.localRotation = Quaternion.identity;
    }


    private void UpdateBeat(float beat)
    {
        if(replayFrames.Count == 0)
        {
            return;
        }

        int lastFrameIndex = replayFrames.GetLastIndex(TimeManager.CurrentTime, x => x.Time <= TimeManager.CurrentTime);
        if(lastFrameIndex < 0)
        {
            SetDefaultPositions();
        }
        else
        {
            ReplayFrame currentFrame = replayFrames[lastFrameIndex];
            headVisual.transform.localPosition = currentFrame.headPosition;
            headVisual.transform.localRotation = currentFrame.headRotation;

            leftSaberVisual.transform.localPosition = currentFrame.leftSaberPosition;
            leftSaberVisual.transform.localRotation = currentFrame.leftSaberRotation;

            rightSaberVisual.transform.localPosition = currentFrame.rightSaberPosition;
            rightSaberVisual.transform.localRotation = currentFrame.rightSaberRotation;
        }
    }


    private void UpdateReplay(Replay newReplay)
    {
        replayFrames.Clear();

        List<Frame> frames = newReplay.frames;
        for(int i = 0; i < frames.Count; i++)
        {
            replayFrames.Add(new ReplayFrame(frames[i]));
        }

        replayFrames.SortElementsByBeat();
    }


    private void UpdateDifficulty(Difficulty newDifficulty)
    {
        if(ReplayManager.IsReplayMode)
        {
            UpdateReplay(ReplayManager.CurrentReplay);
        }
    }


    private void UpdateReplayMode(bool replayMode)
    {
        if(ReplayManager.IsReplayMode)
        {
            TimeManager.OnDifficultyBpmEventsLoaded += UpdateDifficulty;
            TimeManager.OnBeatChanged += UpdateBeat;

            headVisual.SetActive(true);
            leftSaberVisual.SetActive(true);
            rightSaberVisual.SetActive(true);
        }
        else
        {
            replayFrames.Clear();

            headVisual.SetActive(false);
            leftSaberVisual.SetActive(false);
            rightSaberVisual.SetActive(false);
        }
    }


    private void OnEnable()
    {
        ReplayManager.OnReplayModeChanged += UpdateReplayMode;
        UpdateReplayMode(ReplayManager.IsReplayMode);
    }


    private void OnDisable()
    {
        ReplayManager.OnReplayModeChanged -= UpdateReplayMode;
        TimeManager.OnDifficultyBpmEventsLoaded -= UpdateDifficulty;
        TimeManager.OnBeatChanged -= UpdateBeat;

        replayFrames.Clear();
    }
}


public class ReplayFrame : MapElement
{
    public Vector3 headPosition;
    public Quaternion headRotation;

    public Vector3 leftSaberPosition;
    public Quaternion leftSaberRotation;
    
    public Vector3 rightSaberPosition;
    public Quaternion rightSaberRotation;

    public ReplayFrame(Frame f)
    {
        Beat = TimeManager.BeatFromTime(f.time);

        headPosition = f.head.position;
        headRotation = f.head.rotation;

        leftSaberPosition = f.leftHand.position;
        leftSaberRotation = f.leftHand.rotation;

        rightSaberPosition = f.rightHand.position;
        rightSaberRotation = f.rightHand.rotation;
    }
}