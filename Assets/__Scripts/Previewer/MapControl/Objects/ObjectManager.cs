using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    public static ObjectManager Instance { get; private set; }

    [Header("Animation Settings")]
    [SerializeField] public float moveZ;
    [SerializeField] public float moveTime;
    [SerializeField] public float rotationAnimationTime;
    [SerializeField] public float BehindCameraTime = -1f;
    [SerializeField] public float objectFloorOffset;

    [Header("Managers")]
    [SerializeField] public NoteManager noteManager;
    [SerializeField] public BombManager bombManager;
    [SerializeField] public WallManager wallManager;
    [SerializeField] public ChainManager chainManager;
    [SerializeField] public ArcManager arcManager;
    [SerializeField] public JumpManager jumpManager;

    public bool forceGameAccuracy => ReplayManager.IsReplayMode && SettingsManager.GetBool("accuratereplays");

    public bool useSimpleNoteMaterial => SettingsManager.GetBool("simplenotes");
    public bool useSimpleBombMaterial => SettingsManager.GetBool("simplebombs");
    public bool doRotationAnimation => forceGameAccuracy || SettingsManager.GetBool("rotateanimations");
    public bool doMovementAnimation => forceGameAccuracy || SettingsManager.GetBool("moveanimations");
    public bool doFlipAnimation => forceGameAccuracy || SettingsManager.GetBool("flipanimations");

    public bool doLookAnimation => ReplayManager.IsReplayMode && (forceGameAccuracy || SettingsManager.GetBool("lookanimations"));

    public const float DefaultPlayerHeight = 1.8f;
    public float PlayerHeightSetting => SettingsManager.GetFloat("playerheight");
    public float playerHeight => ReplayManager.IsReplayMode ? ReplayManager.PlayerHeight : PlayerHeightSetting;
    public float playerHeightOffset => Mathf.Clamp((playerHeight - DefaultPlayerHeight) / 2, -0.2f, 0.6f);

    public static readonly Vector2 GridBottomLeft = new Vector2(-0.9f, 0);
    public const float LaneWidth = 0.6f;
    public const float RowHeight = 0.55f;
    public const float StartYSpacing = 0.6f;
    public const float WallHScale = 0.6f;
    public const float PrecisionUnits = 0.6f;
    public const float PlayerCutPlaneDistance = 0.65f;

    public static readonly Dictionary<int, float> VanillaRowHeights = new Dictionary<int, float>
    {
        {0, 0},
        {1, 0.55f},
        {2, 1.05f}
    };

    public static readonly Dictionary<int, float> DirectionAngles = new Dictionary<int, float>
    {
        {0, 180},
        {1, 0},
        {2, -90},
        {3, 90},
        {4, -135},
        {5, 135},
        {6, -45},
        {7, 45},
        {8, 0}
    };

    public static readonly Dictionary<int, int> ReverseCutDirection = new Dictionary<int, int>
    {
        {0, 1},
        {1, 0},
        {2, 3},
        {3, 2},
        {4, 7},
        {5, 6},
        {6, 5},
        {7, 4},
        {8, 8}
    };

    public static readonly Dictionary<int, int> MirroredCutDirection = new Dictionary<int, int>
    {
        {0, 0},
        {1, 1},
        {2, 3},
        {3, 2},
        {4, 5},
        {5, 4},
        {6, 7},
        {7, 6},
        {8, 8}
    };

    public float CutPlanePos => ReplayManager.IsReplayMode ? PlayerPositionManager.HeadPosition.z + PlayerCutPlaneDistance : 0f;

    public static event Action OnObjectsLoaded;


    public Vector3 ObjectSpaceToWorldSpace(Vector3 pos)
    {
        pos.y += -objectFloorOffset + 0.25f;
        return pos;
    }


    public static bool CheckSameTime(float time1, float time2)
    {
        const float epsilon = 0.001f;
        return Mathf.Abs(time1 - time2) <= epsilon;
    }


    public Quaternion LookAtPlayer(Vector3 objectPosition, Vector3 headPosition, Quaternion baseRotation, float jumpProgress)
    {
        const float verticalLookStrength = 0.8f;

        headPosition.y = Mathf.Lerp(headPosition.y, objectPosition.y, verticalLookStrength);

        Vector3 headDirection = (objectPosition - headPosition).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(headDirection, baseRotation * Vector3.up);

        return Quaternion.Lerp(baseRotation, lookRotation, jumpProgress);
    }


    public static float MappingExtensionsPrecision(float value)
    {
        //When position values are further than 1000 away, they're on the "precision placement" grid
        if(Mathf.Abs(value) >= 1000)
        {
            value -= 1000 * Mathf.Sign(value);
            value /= 1000;
        }
        return value;
    }


    public static Vector2 MappingExtensionsPosition(Vector2 position)
    {
        position.x = MappingExtensionsPrecision(position.x);
        position.y = MappingExtensionsPrecision(position.y);
        return position;
    }


    public static float? MappingExtensionsAngle(int cutDirection)
    {
        //When the cut direction is above 1000, it's in the "precision angle" space
        if(cutDirection >= 1000)
        {
            float angle = (cutDirection - 1000) % 360;
            if(angle > 180)
            {
                angle -= 360;
            }
            return -angle;
        }
        return null;
    }


    public static Vector2 DirectionVector(float angle)
    {
        return new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), -Mathf.Cos(angle * Mathf.Deg2Rad));
    }


    public static Vector2 CalculateObjectPosition(float x, float y, float[] coordinates = null, bool clampX = true)
    {
        Vector2 position = GridBottomLeft;
        if(coordinates != null && coordinates.Length >= 2)
        {
            //Noodle coordinates treat x differently for some reason
            x = coordinates[0] + 2;
            y = coordinates[1];

            position.x += x * PrecisionUnits;
            position.y += y * PrecisionUnits;
            return position;
        }

        if(BeatmapManager.MappingExtensions)
        {
            Vector2 adjustedPosition = MappingExtensionsPosition(new Vector2(x, y));

            position.x += adjustedPosition.x * PrecisionUnits;
            position.y += adjustedPosition.y * PrecisionUnits;
            return position;
        }

        if(clampX)
        {
            position.x += (int)Mathf.Clamp(x, 0, 3) * LaneWidth;
        }
        else position.x += x * LaneWidth;

        if(ReplayManager.IsReplayMode)
        {
            //Use truly game-accurate y spacing for replays
            //This looks like garbage so I don't use it for regular map viewing
            position.y += VanillaRowHeights[(int)Mathf.Clamp(y, 0, 2)];
        }
        else
        {
            position.y += (int)Mathf.Clamp(y, 0, 2) * RowHeight;
        }

        return position;
    }


    public static float CalculateObjectAngle(int cutDirection, float angleOffset = 0)
    {
        if(BeatmapManager.MappingExtensions)
        {
            float? meAngle = MappingExtensionsAngle(cutDirection);
            if(meAngle != null)
            {
                //Mapping extensions angle applies
                return (float)meAngle;
            }
        }

        float angle = DirectionAngles[Mathf.Clamp(cutDirection, 0, 8)] + angleOffset;
        angle %= 360;
        if(angle > 180)
        {
            angle -= 360;
        }
        else if(angle < -180)
        {
            angle += 360;
        }
        return angle;
    }


    public static bool SamePlaneAngles(float a, float b)
    {
        float diff = Mathf.Abs(a - b);
        return diff.Approximately(0) || diff.Approximately(180);
    }


    public void UpdateDifficulty(Difficulty difficulty)
    {
        HitSoundManager.ClearScheduledSounds();

        jumpManager.UpdateDifficulty(difficulty);

        LoadMapObjects(difficulty.beatmapDifficulty, out noteManager.Objects, out bombManager.Objects, out chainManager.Chains, out arcManager.Objects, out wallManager.Objects);

        noteManager.ReloadNotes();
        chainManager.ReloadChains();
        arcManager.ReloadArcs();
        wallManager.ReloadWalls();
        bombManager.ReloadBombs();

        if(UIStateManager.CurrentState == UIState.Previewer)
        {
            MapStats.UpdateNpsAndSpsValues();
            OnObjectsLoaded?.Invoke();
        }
    }


    public void UpdateBeat(float currentBeat)
    {
        noteManager.UpdateVisuals();
        bombManager.UpdateVisuals();
        wallManager.UpdateVisuals();
        chainManager.UpdateVisuals();
        arcManager.UpdateVisuals();
    }


    public void UpdateColors()
    {
        HitSoundManager.ClearScheduledSounds();

        noteManager.UpdateMaterials();

        bombManager.ReloadBombs();

        chainManager.ClearRenderedVisuals();
        chainManager.UpdateVisuals();

        arcManager.UpdateMaterials();
        wallManager.ReloadWalls();
    }


    public void RescheduleHitsounds(bool playing)
    {
        if(!playing)
        {
            return;
        }

        noteManager.RescheduleHitsounds();
        bombManager.RescheduleHitsounds();
        chainManager.RescheduleHitsounds();
    }


    // only used for loading purposes
    private class BeatmapSliderEnd : BeatmapObject
    {
        public int id; // used for pairing back up
        public float StartY;
        public bool HasAttachment;
        public bool IsHead;

        public float[] coordinates;


        public override void Mirror()
        {
            x = MirrorPosition(x);
        }


        public bool AttachToObject(BeatmapColorNote other)
        {
            return AttachToObject(other.x, other.y, other.customData?.coordinates);
        }


        public bool AttachToObject(BeatmapBombNote other)
        {
            return AttachToObject(other.x, other.y, other.customData?.coordinates);
        }


        public bool AttachToObject(BeatmapBurstSlider other)
        {
            return AttachToObject(other.tx, other.ty, other.customData?.tailCoordinates);
        }


        public bool AttachToObject(int otherX, int otherY, float[] otherCoordinates)
        {
            if(x == otherX && y == otherY)
            {
                return true;
            }

            if(otherCoordinates != null && otherCoordinates.Length >= 2)
            {
                if(coordinates != null && coordinates.Length >= 2)
                {
                    if(coordinates[0].Approximately(otherCoordinates[0]) && coordinates[1].Approximately(otherCoordinates[1]))
                    {
                        return true;
                    }
                }
                else if(Mathf.RoundToInt(otherCoordinates[0] + 2) == x && Mathf.RoundToInt(otherCoordinates[1] + 2) == y)
                {
                    return true;
                }
            }

            return false;
        }
    }


    public static void LoadMapObjects(BeatmapDifficulty beatmapDifficulty, out MapElementList<Note> notes, out MapElementList<Bomb> bombs, out MapElementList<Chain> chains, out MapElementList<Arc> arcs, out MapElementList<Wall> walls)
    {
        // split arcs into heads and tails for easier processing
        List<BeatmapSlider> beatmapSliders = new List<BeatmapSlider>();
        List<BeatmapSliderEnd> beatmapSliderHeads = new List<BeatmapSliderEnd>();
        List<BeatmapSliderEnd> beatmapSliderTails = new List<BeatmapSliderEnd>();
        for(int i = 0; i < beatmapDifficulty.Arcs.Length; i++)
        {
            BeatmapSlider a = beatmapDifficulty.Arcs[i];
            beatmapSliders.Add(a);

            BeatmapSliderEnd head = new BeatmapSliderEnd
            {
                id = i,
                b = a.b,
                x = a.x,
                y = a.y,
                HasAttachment = false,
                IsHead = true,
                coordinates = a.customData?.coordinates
            };

            BeatmapSliderEnd tail = new BeatmapSliderEnd
            {
                id = i,
                b = a.tb,
                x = a.tx,
                y = a.ty,
                HasAttachment = false,
                IsHead = false,
                coordinates = a.customData?.tailCoordinates
            };

            if(a.tb < a.b)
            {
                beatmapSliderTails.Add(head);
                beatmapSliderHeads.Add(tail);
            }
            else
            {
                beatmapSliderHeads.Add(head);
                beatmapSliderTails.Add(tail);
            }
        }

        List<BeatmapObject> allObjects = new List<BeatmapObject>();
        allObjects.AddRange(beatmapDifficulty.Notes);
        allObjects.AddRange(beatmapDifficulty.Bombs);
        allObjects.AddRange(beatmapSliders);
        allObjects.AddRange(beatmapDifficulty.Chains);
        allObjects.AddRange(beatmapDifficulty.Walls);
        allObjects.AddRange(beatmapSliderHeads);
        allObjects.AddRange(beatmapSliderTails);
        allObjects = allObjects.OrderBy(x => x.b).ToList();

        if(ReplayManager.LeftHandedMode)
        {
            foreach(BeatmapObject mapObject in allObjects)
            {
                mapObject.Mirror();
            }
        }

        notes = new MapElementList<Note>();
        bombs = new MapElementList<Bomb>();
        chains = new MapElementList<Chain>();
        arcs = new MapElementList<Arc>();
        walls = new MapElementList<Wall>();

        List<BeatmapObject> sameBeatObjects = new List<BeatmapObject>();
        for(int i = 0; i < allObjects.Count; i++)
        {
            BeatmapObject current = allObjects[i];
            float currentTime = TimeManager.TimeFromBeat(current.b);

            sameBeatObjects.Clear();
            sameBeatObjects.Add(current);

            for(int x = i + 1; x < allObjects.Count; x++)
            {
                //Gather all consecutive objects that share the same beat
                BeatmapObject check = allObjects[x];
                if(CheckSameTime(TimeManager.TimeFromBeat(check.b), currentTime))
                {
                    sameBeatObjects.Add(check);
                    //Skip to the first object that doesn't share this beat next loop
                    i = x;
                }
                else break;
            }

            List<BeatmapColorNote> notesOnBeat = sameBeatObjects.OfType<BeatmapColorNote>().ToList();
            List<BeatmapBombNote> bombsOnBeat = sameBeatObjects.OfType<BeatmapBombNote>().ToList();
            List<BeatmapBurstSlider> burstSlidersOnBeat = sameBeatObjects.OfType<BeatmapBurstSlider>().ToList();
            List<BeatmapObstacle> obstaclesOnBeat = sameBeatObjects.OfType<BeatmapObstacle>().ToList();
            List<BeatmapSliderEnd> sliderEndsOnBeat = sameBeatObjects.OfType<BeatmapSliderEnd>().ToList();

            List<BeatmapObject> notesAndBombs = sameBeatObjects.Where(x => (x is BeatmapColorNote) || (x is BeatmapBombNote)).ToList();

            //Need to pair objects to their replay events if we're in a replay
            List<ScoringEvent> scoringEventsOnBeat = null;
            if(ReplayManager.IsReplayMode)
            {
                scoringEventsOnBeat = ScoreManager.ScoringEvents.FindAll(x => CheckSameTime(x.ObjectTime, currentTime));

                if(ReplayManager.NoArrows)
                {
                    foreach(BeatmapColorNote n in notesOnBeat)
                    {
                        //Force all notes to be dots
                        //It's ok to modify the source beatmap because it's never used again
                        //when a replay is loaded
                        n.d = 8;
                    }
                }
                if(ReplayManager.NoWalls)
                {
                    obstaclesOnBeat.Clear();
                }
                if(ReplayManager.NoBombs)
                {
                    bombsOnBeat.Clear();
                }
            }

            //Precalculate values for all objects on this beat

            //Start by generating the list of chains so notes can attach to them
            List<Chain> newChains = new List<Chain>();
            for(int bi = 0; bi < burstSlidersOnBeat.Count; bi++)
            {
                newChains.Add(new Chain(burstSlidersOnBeat[bi]));
            }

            List<Note> newNotes = new List<Note>();
            (float? redSnapAngle, float? blueSnapAngle) = NoteManager.GetSnapAngles(notesOnBeat);

            //Used to disable swap animations whenever notes are attached to arcs or chains
            bool arcAttachment = false;
            bool chainAttachment = false;
            foreach(BeatmapColorNote n in notesOnBeat)
            {
                Note newNote = new Note(n);
                newNote.StartY = ((float)NoteManager.GetStartY(n, notesAndBombs) * StartYSpacing) + Instance.objectFloorOffset;

                Chain attachedChain = NoteManager.CheckChainHead(n, burstSlidersOnBeat, newChains);
                if(attachedChain != null)
                {
                    //This note is a chain head, let the chain inherit this note's startY
                    newNote.IsChainHead = true;
                    attachedChain.StartY = newNote.StartY;
                }
                chainAttachment |= newNote.IsChainHead;

                // set angle snapping here because angle offset is an int in ColorNote
                if(newNote.Color == 0)
                {
                    newNote.Angle = redSnapAngle ?? newNote.Angle;
                }
                else
                {
                    newNote.Angle = blueSnapAngle ?? newNote.Angle;
                }

                // check attachment to arcs
                bool hasHead = false;
                bool hasTail = false;
                foreach(BeatmapSliderEnd a in sliderEndsOnBeat)
                {
                    if(!a.HasAttachment && a.AttachToObject(n))
                    {
                        a.StartY = newNote.StartY;
                        a.HasAttachment = true;
                        arcAttachment = true;

                        if(a.IsHead)
                        {
                            hasHead = true;
                        }
                        else hasTail = true;
                    }
                }

                if(ReplayManager.IsReplayMode)
                {
                    newNote.EndHeadPosition = PlayerPositionManager.HeadPositionAtTime(newNote.Time);
                    Vector2 worldPosition = Instance.ObjectSpaceToWorldSpace(newNote.Position);

                    //Find the replay event that matches this note
                    //This needs to be done by calculating object ID
                    //scoringType*10000 + lineIndex*1000 + noteLineLayer*100 + colorType*10 + cutDirection
                    ScoringType scoringType;
                    if(newNote.IsChainHead)
                    {
                        if(hasTail)
                        {
                            scoringType = ScoringType.ChainHeadArcTail;
                        }
                        else scoringType = ScoringType.ChainHead;
                    }
                    else if(hasHead)
                    {
                        if(hasTail)
                        {
                            scoringType = ScoringType.ArcHeadArcTail;
                        }
                        else scoringType = ScoringType.ArcHead;
                    }
                    else if(hasTail)
                    {
                        scoringType = ScoringType.ArcTail;
                    }
                    else scoringType = ScoringType.Note;

                    int noteID = ((int)scoringType * 10000) + (n.x * 1000) + (n.y * 100) + (n.c * 10) + n.d;
                    ScoringEvent matchingEvent = scoringEventsOnBeat.Find(x => x.ID == noteID);

                    if(matchingEvent == null)
                    {
                        //Unable to match the note with its "correct" ID, try brute forcing all scoring types
                        matchingEvent = ScoringEvent.BruteForceMatchNote(scoringEventsOnBeat, ref scoringType, noteID, hasTail, hasHead, newNote.IsChainHead);
                    }

                    if(matchingEvent == null || matchingEvent.noteEventType == NoteEventType.miss)
                    {
                        newNote.WasHit = false;
                        newNote.wasMissed = matchingEvent?.noteEventType == NoteEventType.miss;
                    }
                    else
                    {
                        newNote.WasHit = true;
                        newNote.wasMissed = false;
                        newNote.WasBadCut = matchingEvent.noteEventType == NoteEventType.bad;
                        newNote.HitOffset = matchingEvent.HitTimeOffset;
                    }
                    matchingEvent?.SetEventValues(scoringType, worldPosition);

                    //Remove this event so it doesn't get reused by multiple notes
                    scoringEventsOnBeat.Remove(matchingEvent);
                }

                newNotes.Add(newNote);
            }

            if(notesOnBeat.Count == 2 && !arcAttachment && !chainAttachment)
            {
                BeatmapColorNote first = notesOnBeat[0];
                BeatmapColorNote second = notesOnBeat[1];
                (newNotes[0].FlipYHeight, newNotes[1].FlipYHeight) = NoteManager.GetFlipYHeights(first, second);

                if(newNotes[0].FlipYHeight != 0)
                {
                    newNotes[0].FlipStartX = newNotes[1].Position.x;
                }

                if(newNotes[1].FlipYHeight != 0)
                {
                    newNotes[1].FlipStartX = newNotes[0].Position.x;
                }
            }

            notes.AddRange(newNotes);

            foreach(BeatmapBombNote b in bombsOnBeat)
            {
                Bomb newBomb = new Bomb(b);
                newBomb.StartY = (NoteManager.GetStartY(b, notesAndBombs) * StartYSpacing) + Instance.objectFloorOffset;

                // check attachment to arcs
                foreach(BeatmapSliderEnd a in sliderEndsOnBeat)
                {
                    if(!a.HasAttachment && a.AttachToObject(b))
                    {
                        a.StartY = newBomb.StartY;
                        a.HasAttachment = true;
                    }
                }

                if(ReplayManager.IsReplayMode)
                {
                    const int bombColor = 3;
                    const int altBombColor = 0;

                    //Ignore the 1s place since direction doesn't matter
                    int noteID = ((int)ScoringType.NoScore * 10000) + (b.x * 1000) + (b.y * 100) + (bombColor * 10);
                    ScoringEvent matchingEvent = scoringEventsOnBeat.Find(x => x.ID - (x.ID % 10) == noteID);

                    if(matchingEvent == null)
                    {
                        const int altDifference = bombColor - altBombColor;
                        noteID -= altDifference * 10;

                        matchingEvent = scoringEventsOnBeat.Find(x => x.ID - (x.ID % 10) == noteID);
                    }

                    if(matchingEvent != null && matchingEvent.noteEventType == NoteEventType.bomb)
                    {
                        newBomb.WasHit = true;
                        newBomb.WasBadCut = true;
                        newBomb.HitOffset = matchingEvent.ObjectTime - matchingEvent.Time;

                        Vector2 worldPosition = Instance.ObjectSpaceToWorldSpace(newBomb.Position);
                        matchingEvent.SetEventValues(ScoringType.NoScore, worldPosition);
                    }
                    scoringEventsOnBeat.Remove(matchingEvent);
                }

                bombs.Add(newBomb);
            }

            //Check arc attachment to chain tails
            for(int bi = 0; bi < burstSlidersOnBeat.Count; bi++)
            {
                BeatmapBurstSlider b = burstSlidersOnBeat[bi];
                Chain chain = newChains[bi];

                foreach(BeatmapSliderEnd a in sliderEndsOnBeat)
                {
                    if(!a.HasAttachment && a.AttachToObject(b))
                    {
                        a.StartY = chain.StartY;
                        a.HasAttachment = true;
                    }
                }
            }
            chains.AddRange(newChains);

            foreach(BeatmapObstacle o in obstaclesOnBeat)
            {
                Wall newWall = new Wall(o);
                walls.Add(newWall);
            }
        }

        // pair slider heads/tails back up and make final arcs
        for(int i = 0; i < beatmapSliderHeads.Count; i++)
        {
            const float halfNoteOffset = 0.225f;

            BeatmapSliderEnd head = beatmapSliderHeads[i];
            BeatmapSliderEnd tail = beatmapSliderTails[i];

            Arc newArc = new Arc(beatmapSliders[i]);

            if(head.HasAttachment)
            {
                Vector2 offset = newArc.HeadOffsetDirection * halfNoteOffset;
                newArc.Position += offset;
                newArc.HeadControlPoint += offset;
                newArc.HeadStartY = head.StartY + offset.y;
                newArc.HasHeadAttachment = true;
            }
            if(tail.HasAttachment)
            {
                Vector2 offset = newArc.TailOffsetDirection * halfNoteOffset;
                newArc.TailPosition += offset;
                newArc.TailControlPoint += offset;
                newArc.TailStartY = tail.StartY + offset.y;
            }

            arcs.Add(newArc);
        }

        //This is necessary because negative duration objects won't be sorted properly
        walls.SortElementsByBeat();
        arcs.SortElementsByBeat();
    }


    private void Awake()
    {
        if(Instance && Instance != this)
        {
            Debug.Log("Duplicate ObjectManager in scene.");
            enabled = false;
        }
        else Instance = this;
    }


    private void Start()
    {
        //Using this event instead of BeatmapManager.OnDifficultyChanged
        //ensures that bpm changes are loaded before precalculating object times
        TimeManager.OnDifficultyBpmEventsLoaded += UpdateDifficulty;
        TimeManager.OnBeatChanged += UpdateBeat;
        TimeManager.OnPlayingChanged += RescheduleHitsounds;

        ColorManager.OnColorsChanged += (_) => UpdateColors();
    }


    private void OnDisable()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }
}


public abstract class MapObject : MapElement
{   
    public GameObject Visual;
    public Vector2 Position;
    public Color? CustomColor;
    public float? CustomNJS;
    public float? CustomRT;
}


public abstract class HitSoundEmitter : MapObject
{
    public AudioSource source;
    public bool WasHit;
    public bool wasMissed;
    public bool WasBadCut;
    public float HitOffset;
}


public abstract class BaseSlider : MapObject
{
    private float _tailBeat;
    public float TailBeat
    {
        get => _tailBeat;
        set
        {
            _tailBeat = value;
            TailTime = TimeManager.TimeFromBeat(_tailBeat);
        }
    }
    public float TailTime { get; private set; }

    public int Color;
    public Vector2 TailPosition;
}


public abstract class MapElementManager<T> : MonoBehaviour where T : MapElement
{
    public MapElementList<T> Objects = new MapElementList<T>();
    public MapElementList<T> CustomRTObjects = new MapElementList<T>();
    public List<T> RenderedObjects = new List<T>();

    public ObjectManager objectManager => ObjectManager.Instance;
    public JumpManager jumpManager => objectManager.jumpManager;

    public abstract void UpdateVisual(T visual);
    public abstract bool VisualInSpawnRange(T visual);
    public abstract void ReleaseVisual(T visual);

    public abstract void UpdateObjects(MapElementList<T> objects);
    public abstract float GetSpawnTime(T visual);


    public virtual void UpdateVisuals()
    {
        ClearOutsideVisuals();
        UpdateObjects(Objects);
        UpdateObjects(CustomRTObjects);
    }


    public virtual void ClearOutsideVisuals()
    {
        for(int i = RenderedObjects.Count - 1; i >= 0; i--)
        {
            T visual = RenderedObjects[i];
            if(!VisualInSpawnRange(visual))
            {
                ReleaseVisual(visual);
                RenderedObjects.Remove(visual);
            }
        }
    }


    public void ClearRenderedVisuals()
    {
        foreach(T visual in RenderedObjects)
        {
            ReleaseVisual(visual);
        }
        RenderedObjects.Clear();
    }


    public int GetStartIndex(float currentTime, MapElementList<T> objects) => objects.GetFirstIndex(currentTime, VisualInSpawnRange);
}