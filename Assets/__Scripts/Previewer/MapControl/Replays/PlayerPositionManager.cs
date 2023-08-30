using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerPositionManager : MonoBehaviour
{
    public static MapElementList<ReplayFrame> ReplayFrames = new MapElementList<ReplayFrame>();

    public static Vector3 HeadPosition { get; private set; }
    public static Quaternion HeadRotation { get; private set; }

    public static float Energy { get; private set; }
    public static int AverageFPS { get; private set; }

    [Header("Components")]
    [SerializeField] private HeadsetHandler headset;
    [SerializeField] private SaberHandler leftSaber;
    [SerializeField] private SaberHandler rightSaber;
    [SerializeField] private GameObject playerPlatform;

    [Header("Positions")]
    [SerializeField] private Vector3 defaultHmdPosition;
    [SerializeField] private Vector3 defaultLeftSaberPosition;
    [SerializeField] private Vector3 defaultRightSaberPosition;

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
        "sabertrailwidth",
        "sabertrailsegments"
    };

    private bool useTrails => SettingsManager.GetBool("sabertrails");
    private int trailIndex => Mathf.Clamp(SettingsManager.GetInt("sabertrailtype"), 0, trailTextures.Length - 1);


    public static Vector3 PlayerSpaceToWorldSpace(Vector3 pos)
    {
        pos.z -= ObjectManager.PlayerCutPlaneDistance;
        return pos;
    }


    public static Vector3 HeadPositionAtTime(float time, bool worldSpace = true)
    {
        if(ReplayFrames.Count == 0)
        {
            return Vector3.zero;
        }

        int lastFrameIndex = ReplayFrames.GetLastIndexUnoptimized(x => x.Time <= time);
        if(lastFrameIndex < 0)
        {
            //Always start with the first frame
            lastFrameIndex = 0;
        }

        Vector3 headPosition = ReplayFrames[lastFrameIndex].headPosition;
        if(worldSpace)
        {
            return PlayerSpaceToWorldSpace(headPosition);
        }
        else return headPosition;
    }


    private void SetDefaultPositions()
    {
        headset.transform.localPosition = defaultHmdPosition;
        headset.transform.localRotation = Quaternion.identity;

        leftSaber.transform.localPosition = defaultLeftSaberPosition;
        leftSaber.transform.localRotation = Quaternion.identity;

        rightSaber.transform.localPosition = defaultRightSaberPosition;
        rightSaber.transform.localRotation = Quaternion.identity;

        HeadPosition = PlayerSpaceToWorldSpace(defaultHmdPosition);
        HeadRotation = Quaternion.identity;
    }


    private void UpdateBeat(float beat)
    {
        if(ReplayFrames.Count == 0)
        {
            SetDefaultPositions();
            return;
        }

        int lastFrameIndex = ReplayFrames.GetLastIndex(TimeManager.CurrentTime, x => x.Time <= TimeManager.CurrentTime);
        if(lastFrameIndex < 0)
        {
            //Always start with the first frame
            lastFrameIndex = 0;
        }

        //Lerp between frames to keep the visuals smooth
        ReplayFrame currentFrame = ReplayFrames[lastFrameIndex];
        ReplayFrame nextFrame = lastFrameIndex + 1 < ReplayFrames.Count ? ReplayFrames[lastFrameIndex + 1] : currentFrame;

        float timeDifference = nextFrame.Time - currentFrame.Time;
        float t = timeDifference <= 0 ? 0f : (TimeManager.CurrentTime - currentFrame.Time) / timeDifference;

        headset.transform.localPosition = Vector3.Lerp(currentFrame.headPosition, nextFrame.headPosition, t);
        headset.transform.localRotation = Quaternion.Lerp(currentFrame.headRotation, nextFrame.headRotation, t);

        bool leftSaberActive = leftSaber.gameObject.activeInHierarchy;
        bool rightSaberActive = rightSaber.gameObject.activeInHierarchy;

        if(leftSaberActive)
        {
            leftSaber.transform.localPosition = Vector3.Lerp(currentFrame.leftSaberPosition, nextFrame.leftSaberPosition, t);
            leftSaber.transform.localRotation = Quaternion.Lerp(currentFrame.leftSaberRotation, nextFrame.leftSaberRotation, t);
        }
        if(rightSaberActive)
        {
            rightSaber.transform.localPosition = Vector3.Lerp(currentFrame.rightSaberPosition, nextFrame.rightSaberPosition, t);
            rightSaber.transform.localRotation = Quaternion.Lerp(currentFrame.rightSaberRotation, nextFrame.rightSaberRotation, t);
        }

        if(useTrails)
        {
            if(leftSaberActive)
            {
                leftSaber.SetFrames(ReplayFrames, lastFrameIndex);
            }
            if(rightSaberActive)
            {
                rightSaber.SetFrames(ReplayFrames, lastFrameIndex);
            }
        }

        Energy = currentFrame.Energy;
        AverageFPS = currentFrame.AverageFPS;

        HeadPosition = PlayerSpaceToWorldSpace(currentFrame.headPosition);
        HeadRotation = currentFrame.headRotation;
    }


    private void UpdateReplay(Replay newReplay)
    {
        ReplayFrames.Clear();

        float lastCheckedFramerateTime = 0f;
        int checkedFrameCount = 0;
        int totalFPS = 0;
        int averageFramerate = 0;

        List<Frame> frames = newReplay.frames;
        for(int i = 0; i < frames.Count; i++)
        {
            ReplayFrame newFrame = new ReplayFrame(frames[i]);
            if(i > 1)
            {
                ReplayFrame lastFrame = ReplayFrames[i - 1];
                ReplayFrame secondLastFrame = ReplayFrames[i - 2];

                //DeltaTime simulates Time.deltaTime, which has a one frame delay
                //So we get the time difference between the last two frames
                newFrame.DeltaTime = lastFrame.Time - secondLastFrame.Time;
            }
            else
            {
                //DeltaTime can be approximated based on framerate
                newFrame.DeltaTime = 1f / newFrame.FPS;
            }

            //Calculate average framerates for displaying on the frame counter
            checkedFrameCount++;
            totalFPS += newFrame.FPS;

            if(i == 0)
            {
                averageFramerate = newFrame.FPS;
            }
            else
            {
                float timeDifference = newFrame.Time - lastCheckedFramerateTime;
                if(timeDifference >= FpsDisplay.FramerateSampleTime)
                {
                    averageFramerate = totalFPS / checkedFrameCount;

                    lastCheckedFramerateTime = newFrame.Time;
                    checkedFrameCount = 0;
                    totalFPS = 0;
                }
            }
            newFrame.AverageFPS = averageFramerate;

            ReplayFrames.Add(newFrame);
        }

        if(ReplayManager.BatteryEnergy || ReplayManager.OneLife)
        {
            Energy = 1f;
        }
        else Energy = 0.5f;

        ReplayFrames.SortElementsByBeat();
        UpdateSaberMaterials();
        UpdateSettings("all");
    }


    private void UpdateReplayMode(bool replayMode)
    {
        if(ReplayManager.IsReplayMode)
        {
            TimeManager.OnBeatChangedEarly += UpdateBeat;

            playerPlatform.SetActive(true);
            headset.gameObject.SetActive(true);
            leftSaber.gameObject.SetActive(true);
            rightSaber.gameObject.SetActive(true);

            UpdateReplay(ReplayManager.CurrentReplay);
        }
        else
        {
            ReplayFrames.Clear();

            playerPlatform.SetActive(false);
            headset.gameObject.SetActive(false);
            leftSaber.gameObject.SetActive(false);
            rightSaber.gameObject.SetActive(false);
        }
    }


    private void UpdateDifficulty(Difficulty newDifficulty)
    {
        if(!ReplayManager.IsReplayMode)
        {
            leftSaber.gameObject.SetActive(false);
            rightSaber.gameObject.SetActive(false);
            return;
        }

        if(ReplayManager.OneSaber)
        {
            leftSaber.gameObject.SetActive(ReplayManager.LeftHandedMode);
            rightSaber.gameObject.SetActive(!ReplayManager.LeftHandedMode);
        }
        else
        {
            leftSaber.gameObject.SetActive(true);
            rightSaber.gameObject.SetActive(true);
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
        if(!ReplayManager.IsReplayMode)
        {
            return;
        }

        UpdateSaberMaterials();
        UpdateTrailMaterials();
    }


    private void UpdateSettings(string setting)
    {
        if(!ReplayManager.IsReplayMode)
        {
            return;
        }

        bool allSettings = setting == "all";
        if(allSettings || trailMaterialSettings.Contains(setting))
        {
            UpdateTrailMaterials();
        }
        if(allSettings || redrawSettings.Contains(setting))
        {
            UpdateBeat(TimeManager.CurrentBeat);

            bool trail = useTrails; //Just to avoid an unnecessary extra settings lookup
            leftSaber.SetTrailActive(trail);
            rightSaber.SetTrailActive(trail);
        }
        if(allSettings || setting == "saberwidth")
        {
            float width = SettingsManager.GetFloat("saberwidth");
            leftSaber.SetWidth(width);
            rightSaber.SetWidth(width);
        }
        if(allSettings || setting == "showheadset" || setting == "firstpersonreplay")
        {
            bool enableHeadset = SettingsManager.GetBool("showheadset") && !SettingsManager.GetBool("firstpersonreplay");
            headset.gameObject.SetActive(enableHeadset);
        }
        if(allSettings || setting == "headsetalpha")
        {
            headset.SetAlpha(SettingsManager.GetFloat("headsetalpha"));
        }
    }


    private static float DamageAmountFromEvent(ScoringEvent scoringEvent)
    {
        const float badcutDamageAmount = 0.1f;
        const float chainLinkBadcutDamageAmount = 0.025f;
        const float missDamageAmount = 0.15f;
        const float chainLinkMissDamageAmount = 0.03f;

        if(scoringEvent.IsWall)
        {
            return 0f;
        }

        switch(scoringEvent.noteEventType)
        {
            case NoteEventType.bad:
                if(scoringEvent.scoringType == ScoringType.ChainLink)
                {
                    return chainLinkBadcutDamageAmount;
                }
                else return badcutDamageAmount;

            case NoteEventType.miss:
            case NoteEventType.bomb:
            default:
                if(scoringEvent.scoringType == ScoringType.ChainLink)
                {
                    return chainLinkMissDamageAmount;
                }
                else return missDamageAmount;
        }
    }


    public static void InitializeEnergyValues(MapElementList<ScoringEvent> scoringEvents)
    {
        const float wallDamagePerSecond = 1.3f;

        const float healAmount = 0.01f;
        const float chainLinkHealAmount = 0.002f;

        const int batteryLives = 4;

        bool batteryEnergy = ReplayManager.BatteryEnergy;
        bool oneLife = ReplayManager.OneLife;

        float energy = 0.5f;
        if(batteryEnergy || oneLife)
        {
            //Energy starts at full for the energy modifiers
            energy = 1f;
        }

        bool headInWall = false;
        float wallExitEnergy = 0f;
        
        bool initailizedEnergy = false;

        int scoringEventIndex = 0;
        for(int i = 0; i < ReplayFrames.Count; i++)
        {
            ReplayFrame currentFrame = ReplayFrames[i];

            //Apply any scoring events that have taken effect since the last frame
            for(int x = scoringEventIndex; x < scoringEvents.Count; x++)
            {
                ScoringEvent newEvent = scoringEvents[x];
                if(newEvent.Time > currentFrame.Time)
                {
                    break;
                }

                if(oneLife)
                {
                    if(newEvent.IsBadHit || newEvent.IsWall)
                    {
                        //Immediately fail on the first bad hit
                        energy = 0f;
                        break;
                    }
                }
                else if(batteryEnergy)
                {
                    if(newEvent.IsBadHit || newEvent.IsWall)
                    {
                        energy -= 1f / batteryLives;
                    }
                }
                else
                {
                    if(newEvent.IsWall)
                    {
                        //The player entered a wall, start draining energy
                        //Energy draining stops once we reach wallExitEnergy
                        headInWall = true;
                        wallExitEnergy = newEvent.WallExitEnergy;
                    }
                    else if(newEvent.IsBadHit)
                    {
                        energy -= DamageAmountFromEvent(newEvent);
                    }
                    else
                    {
                        //Recover energy on good hits, when there are no modifiers
                        energy += newEvent.scoringType == ScoringType.ChainLink ? chainLinkHealAmount : healAmount;
                    }
                }

                scoringEventIndex = x + 1;
            }

            if(headInWall)
            {
                //Once our energy is below the end value of the wall event, we've left the wall
                //This is super scuffed because the replay files are DUMB !!11 !
                if(energy <= wallExitEnergy)
                {
                    headInWall = false;
                }
                else energy -= wallDamagePerSecond * currentFrame.DeltaTime;
            }

            if(energy <= 0)
            {
                ReplayManager.Failed = true;
                ReplayManager.FailTime = currentFrame.Time;

                //We can stop here because frames get initialized to 0 energy
                //and energy will always be 0 after failing
                break;
            }

            energy = Mathf.Min(energy, 1f);
            currentFrame.Energy = energy;

            if(!initailizedEnergy && currentFrame.Time >= TimeManager.CurrentTime)
            {
                //Initialize the global energy value with the first frame
                Energy = energy;
                initailizedEnergy = true;
            }
        }
    }


    private void OnEnable()
    {
        ReplayManager.OnReplayModeChanged += UpdateReplayMode;
        ColorManager.OnColorsChanged += UpdateColors;
        SettingsManager.OnSettingsUpdated += UpdateSettings;
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateDifficulty;

        UpdateReplayMode(ReplayManager.IsReplayMode);
    }


    private void OnDisable()
    {
        TimeManager.OnBeatChangedEarly -= UpdateBeat;

        ReplayManager.OnReplayModeChanged -= UpdateReplayMode;
        ColorManager.OnColorsChanged -= UpdateColors;
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateDifficulty;

        ReplayFrames.Clear();
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

    public float DeltaTime;
    public float Energy;
    public int FPS;
    public int AverageFPS;

    public ReplayFrame(Frame f)
    {
        Time = f.time;

        headPosition = f.head.position;
        headRotation = f.head.rotation;

        leftSaberPosition = f.leftHand.position;
        leftSaberRotation = f.leftHand.rotation;

        rightSaberPosition = f.rightHand.position;
        rightSaberRotation = f.rightHand.rotation;

        DeltaTime = 0f;
        Energy = 0f;
        FPS = f.fps;
    }
}