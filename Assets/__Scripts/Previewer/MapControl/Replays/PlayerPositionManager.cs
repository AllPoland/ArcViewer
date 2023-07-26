using System.Collections.Generic;
using UnityEngine;

public class PlayerPositionManager : MonoBehaviour
{
    public static MapElementList<ReplayFrame> replayFrames = new MapElementList<ReplayFrame>();

    public static Vector3 HeadPosition { get; private set; }
    public static Vector3 LeftSaberTipPosition { get; private set; }
    public static Vector3 RightSaberTipPosition { get; private set; }

    [SerializeField] private GameObject headVisual;
    [SerializeField] private GameObject leftSaberVisual;
    [SerializeField] private GameObject rightSaberVisual;
    [SerializeField] private GameObject playerPlatform;

    [Space]
    [SerializeField] private Vector3 defaultHmdPosition;
    [SerializeField] private Vector3 defaultLeftSaberPosition;
    [SerializeField] private Vector3 defaultRightSaberPosition;

    [Space]
    [SerializeField] private float saberTipOffset;


    public static Vector3 HeadPositionAtTime(float time)
    {
        if(replayFrames.Count == 0)
        {
            return Vector3.zero;
        }

        int lastFrameIndex = replayFrames.GetLastIndexUnoptimized(x => x.Time <= time);
        if(lastFrameIndex < 0)
        {
            //Always start with the first frame
            lastFrameIndex = 0;
        }

        return replayFrames[lastFrameIndex].headPosition;
    }


    private void SetDefaultPositions()
    {
        headVisual.transform.localPosition = defaultHmdPosition;
        headVisual.transform.localRotation = Quaternion.identity;

        leftSaberVisual.transform.localPosition = defaultLeftSaberPosition;
        leftSaberVisual.transform.localRotation = Quaternion.identity;

        rightSaberVisual.transform.localPosition = defaultRightSaberPosition;
        rightSaberVisual.transform.localRotation = Quaternion.identity;

        UpdatePositions();
    }


    private void UpdateBeat(float beat)
    {
        if(replayFrames.Count == 0)
        {
            SetDefaultPositions();
            return;
        }

        int lastFrameIndex = replayFrames.GetLastIndex(TimeManager.CurrentTime, x => x.Time <= TimeManager.CurrentTime);
        if(lastFrameIndex < 0)
        {
            //Always start with the first frame
            lastFrameIndex = 0;
        }

        //Lerp between frames to keep the visuals smooth
        ReplayFrame currentFrame = replayFrames[lastFrameIndex];
        ReplayFrame nextFrame = lastFrameIndex + 1 < replayFrames.Count ? replayFrames[lastFrameIndex + 1] : currentFrame;

        float timeDifference = nextFrame.Time - currentFrame.Time;
        float t = (TimeManager.CurrentTime - currentFrame.Time) / timeDifference;

        headVisual.transform.localPosition = Vector3.Lerp(currentFrame.headPosition, nextFrame.headPosition, t);
        headVisual.transform.localRotation = Quaternion.Lerp(currentFrame.headRotation, nextFrame.headRotation, t);

        leftSaberVisual.transform.localPosition = Vector3.Lerp(currentFrame.leftSaberPosition, nextFrame.leftSaberPosition, t);
        leftSaberVisual.transform.localRotation = Quaternion.Lerp(currentFrame.leftSaberRotation, nextFrame.leftSaberRotation, t);

        rightSaberVisual.transform.localPosition = Vector3.Lerp(currentFrame.rightSaberPosition, nextFrame.rightSaberPosition, t);
        rightSaberVisual.transform.localRotation = Quaternion.Lerp(currentFrame.rightSaberRotation, nextFrame.rightSaberRotation, t);

        UpdatePositions();
    }


    private void UpdatePositions()
    {
        HeadPosition = headVisual.transform.position;
        LeftSaberTipPosition = leftSaberVisual.transform.position + (leftSaberVisual.transform.forward * saberTipOffset);
        RightSaberTipPosition = rightSaberVisual.transform.position + (rightSaberVisual.transform.forward * saberTipOffset);
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
        UpdateBeat(0f);
    }


    private void UpdateReplayMode(bool replayMode)
    {
        if(ReplayManager.IsReplayMode)
        {
            TimeManager.OnBeatChangedEarly += UpdateBeat;

            headVisual.SetActive(true);
            leftSaberVisual.SetActive(true);
            rightSaberVisual.SetActive(true);
            playerPlatform.SetActive(true);
            UpdateReplay(ReplayManager.CurrentReplay);
        }
        else
        {
            replayFrames.Clear();

            headVisual.SetActive(false);
            leftSaberVisual.SetActive(false);
            rightSaberVisual.SetActive(false);
            playerPlatform.SetActive(false);
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
        TimeManager.OnBeatChangedEarly -= UpdateBeat;

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
        Time = f.time;

        headPosition = f.head.position;
        headRotation = f.head.rotation;

        leftSaberPosition = f.leftHand.position;
        leftSaberRotation = f.leftHand.rotation;

        rightSaberPosition = f.rightHand.position;
        rightSaberRotation = f.rightHand.rotation;
    }
}