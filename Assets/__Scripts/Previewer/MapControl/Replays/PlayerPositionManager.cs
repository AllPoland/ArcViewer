using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerPositionManager : MonoBehaviour
{
    public static MapElementList<ReplayFrame> replayFrames = new MapElementList<ReplayFrame>();

    public static Vector3 HeadPosition { get; private set; }
    public static Vector3 LeftSaberTipPosition { get; private set; }
    public static Vector3 RightSaberTipPosition { get; private set; }

    [Header("Components")]
    [SerializeField] private GameObject headVisual;
    [SerializeField] private SaberHandler leftSaber;
    [SerializeField] private SaberHandler rightSaber;
    [SerializeField] private GameObject playerPlatform;

    [Header("Positions")]
    [SerializeField] private Vector3 defaultHmdPosition;
    [SerializeField] private Vector3 defaultLeftSaberPosition;
    [SerializeField] private Vector3 defaultRightSaberPosition;

    [Space]
    [SerializeField] private float saberTipOffset;

    [Header("Visuals")]
    [SerializeField] private Texture2D[] trailTextures;

    private string[] trailMaterialSettings = new string[]
    {
        "sabertrails",
        "sabertrailtype",
        "sabertrailbrightness"
    };

    private string[] redrawSettings = new string[]
    {
        "sabertrails",
        "sabertraillength",
        "sabertrailsegments"
    };

    private bool useTrails => SettingsManager.GetBool("sabertrails");
    private int trailIndex => Mathf.Clamp(SettingsManager.GetInt("sabertrailtype"), 0, trailTextures.Length - 1);


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

        leftSaber.transform.localPosition = defaultLeftSaberPosition;
        leftSaber.transform.localRotation = Quaternion.identity;

        rightSaber.transform.localPosition = defaultRightSaberPosition;
        rightSaber.transform.localRotation = Quaternion.identity;

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

        leftSaber.transform.localPosition = Vector3.Lerp(currentFrame.leftSaberPosition, nextFrame.leftSaberPosition, t);
        leftSaber.transform.localRotation = Quaternion.Lerp(currentFrame.leftSaberRotation, nextFrame.leftSaberRotation, t);

        rightSaber.transform.localPosition = Vector3.Lerp(currentFrame.rightSaberPosition, nextFrame.rightSaberPosition, t);
        rightSaber.transform.localRotation = Quaternion.Lerp(currentFrame.rightSaberRotation, nextFrame.rightSaberRotation, t);

        if(useTrails)
        {
            leftSaber.SetFrames(replayFrames, lastFrameIndex);
            rightSaber.SetFrames(replayFrames, lastFrameIndex);
        }

        UpdatePositions();
    }


    private void UpdatePositions()
    {
        HeadPosition = headVisual.transform.position;
        LeftSaberTipPosition = leftSaber.transform.position + (leftSaber.transform.forward * saberTipOffset);
        RightSaberTipPosition = rightSaber.transform.position + (rightSaber.transform.forward * saberTipOffset);
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
            leftSaber.gameObject.SetActive(true);
            rightSaber.gameObject.SetActive(true);
            playerPlatform.SetActive(true);

            UpdateReplay(ReplayManager.CurrentReplay);
            UpdateSaberMaterials();
        }
        else
        {
            replayFrames.Clear();

            headVisual.SetActive(false);
            leftSaber.gameObject.SetActive(false);
            rightSaber.gameObject.SetActive(false);
            playerPlatform.SetActive(false);
        }
    }


    public void UpdateTrailMaterials()
    {
        if(!useTrails)
        {
            return;
        }

        float brightness = SettingsManager.GetFloat("sabertrailbrightness");
        Texture2D trail = trailTextures[trailIndex];

        leftSaber.SetTrailProperties(NoteManager.RedNoteColor, brightness, trail);
        rightSaber.SetTrailProperties(NoteManager.BlueNoteColor, brightness, trail);
    }


    public void UpdateSaberMaterials()
    {
        leftSaber.SetSaberProperties(NoteManager.RedNoteColor);
        rightSaber.SetSaberProperties(NoteManager.BlueNoteColor);
    }


    public void UpdateColors(ColorPalette _)
    {
        UpdateSaberMaterials();
        UpdateTrailMaterials();
    }


    private void UpdateSettings(string changedSetting)
    {
        bool allSettings = changedSetting == "all";
        if(allSettings || trailMaterialSettings.Contains(changedSetting))
        {
            UpdateTrailMaterials();
        }
        if(allSettings || redrawSettings.Contains(changedSetting))
        {
            UpdateBeat(TimeManager.CurrentBeat);

            bool trail = useTrails; //Just to avoid an unnecessary extra settings lookup
            leftSaber.SetTrailActive(trail);
            rightSaber.SetTrailActive(trail);
        }
    }


    private void OnEnable()
    {
        ReplayManager.OnReplayModeChanged += UpdateReplayMode;
        ColorManager.OnColorsChanged += UpdateColors;
        SettingsManager.OnSettingsUpdated += UpdateSettings;

        UpdateReplayMode(ReplayManager.IsReplayMode);
    }


    private void OnDisable()
    {
        ReplayManager.OnReplayModeChanged -= UpdateReplayMode;
        TimeManager.OnBeatChangedEarly -= UpdateBeat;
        ColorManager.OnColorsChanged -= UpdateColors;
        SettingsManager.OnSettingsUpdated -= UpdateSettings;

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