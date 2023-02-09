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
    [SerializeField] public float behindCameraZ;
    [SerializeField] public float objectFloorOffset;

    [Header("Managers")]
    [SerializeField] private NoteManager noteManager;
    [SerializeField] private WallManager wallManager;
    [SerializeField] private ChainManager chainManager;
    [SerializeField] private ArcManager arcManager;

    public bool useSimpleNoteMaterial = false;
    public bool doRotationAnimation = true;
    public bool doMovementAnimation = true;

    public static readonly Vector2 gridBottomLeft = new Vector2(-0.9f, 0);
    public const float laneWidth = 0.6f;
    public const float rowHeight = 0.55f;
    public const float wallHScale = 0.6f;

    public float BehindCameraTime
    {
        get
        {
            return TimeFromWorldspace(behindCameraZ);
        }
    }


    public static List<T> SortObjectsByBeat<T>(List<T> objects) where T : MapObject
    {
        return objects.OrderBy(x => x.Beat).ToList();
    }


    public static List<T> GetObjectsOnBeat<T>(List<T> search, float beat) where T : MapObject
    {
        const float leeway = 0.001f;
        float time = TimeManager.TimeFromBeat(beat);

        return search.FindAll(x => Mathf.Abs(TimeManager.TimeFromBeat(x.Beat) - time) <= leeway);
    }


    public static bool CheckSameBeat(float beat1, float beat2)
    {
        const float leeway = 0.001f;
        return Mathf.Abs(TimeManager.TimeFromBeat(beat1) - TimeManager.TimeFromBeat(beat2)) <= leeway;
    }


    public bool CheckInSpawnRange(float beat)
    {
        float time = TimeManager.TimeFromBeat(beat);
        return
        (
            time > TimeManager.CurrentTime &&
            time <= TimeManager.CurrentTime + BeatmapManager.ReactionTime + Instance.moveTime
        );
    }


    public bool CheckInSpawnRange(float beat, float currentBeat)
    {
        float time = TimeManager.TimeFromBeat(beat);
        float currentTime = TimeManager.TimeFromBeat(currentBeat);
        return
        (
            time > currentTime &&
            time <= currentTime + BeatmapManager.ReactionTime + Instance.moveTime
        );
    }


    public bool DurationObjectInSpawnRange(float startBeat, float endBeat)
    {
        float startTime = TimeManager.TimeFromBeat(startBeat);
        float endTime = TimeManager.TimeFromBeat(endBeat) - BehindCameraTime;

        bool timeInRange = TimeManager.CurrentTime > startTime && TimeManager.CurrentTime <= endTime;
        bool jumpTime = CheckInSpawnRange(startBeat);

        return jumpTime || timeInRange;
    }


    public float GetZPosition(float objectTime)
    {
        float reactionTime = BeatmapManager.ReactionTime;
        float jumpTime = TimeManager.CurrentTime + reactionTime;

        if(objectTime <= jumpTime)
        {
            //Note has jumped in. Place based on Jump Setting stuff
            float timeDist = objectTime - TimeManager.CurrentTime;
            return WorldSpaceFromTime(timeDist);
        }
        else
        {
            //Note hasn't jumped in yet. Place based on the jump-in stuff
            float timeDist = (objectTime - jumpTime) / moveTime;
            timeDist = Easings.Quad.Out(timeDist);
            return (BeatmapManager.JumpDistance / 2) + (moveZ * timeDist);
        }
    }


    public float WorldSpaceFromTime(float time)
    {
        float NJS = BeatmapManager.CurrentMap.NoteJumpSpeed;
        return time * NJS;
    }


    public float TimeFromWorldspace(float position)
    {
        float NJS = BeatmapManager.CurrentMap.NoteJumpSpeed;
        return position / NJS;
    }


    public static float SpawnParabola(float targetHeight, float baseHeight, float halfJumpDistance, float t)
    {
        float dSquared = Mathf.Pow(halfJumpDistance, 2);
        float tSquared = Mathf.Pow(t, 2);

        float movementRange = targetHeight - baseHeight;

        return -(movementRange / dSquared) * tSquared + targetHeight;
    }


    public float GetObjectY(float startY, float targetY, float objectTime)
    {
        float jumpTime = TimeManager.CurrentTime + BeatmapManager.ReactionTime;

        if(objectTime > jumpTime)
        {
            return startY;
        }
        else if(objectTime < TimeManager.CurrentTime)
        {
            return targetY;
        }

        float halfJumpDistance = BeatmapManager.JumpDistance / 2;
        return SpawnParabola(targetY, startY, halfJumpDistance, GetZPosition(objectTime));
    }

    public static Vector2 DirectionVector(float angle)
    {
        return new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), -Mathf.Cos(angle * Mathf.Deg2Rad));
    }


    public static Vector2 CalculateObjectPosition(int x, int y)
    {
        Vector2 position = gridBottomLeft;
        position.x += x * laneWidth;
        position.y += y * rowHeight;
        return position;
    }


    public static float CalculateObjectAngle(int cutDirection, float angleOffset = 0)
    {
        Dictionary<int, float> DirectionAngles = new Dictionary<int, float>
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
        return DirectionAngles[Mathf.Clamp(cutDirection, 0, 8)] + angleOffset;
    }


    public void UpdateManagers(Difficulty difficulty)
    {
        HitSoundManager.ClearScheduledSounds();

        LoadMapObjects(difficulty.beatmapDifficulty, out noteManager.Notes, out noteManager.Bombs, out chainManager.Chains, out arcManager.Arcs, out wallManager.Walls);

        chainManager.ReloadChains();
        noteManager.ReloadNotes();
        arcManager.ReloadArcs();
        wallManager.ReloadWalls();
    }


    // only used for loading purposes
    private class BeatmapSliderHead : BeatmapObject
    {
        public int id; // used for pairing back up
        public int c;
        public int d;
        public float mu;
        public int m;
        public float StartY;
        public bool HasAttachment;

        public BeatmapCustomSliderData customData;
    }

    private class BeatmapSliderTail : BeatmapObject
    {
        public int id; // used for pairing back up
        public int d;
        public float mu;
        public float StartY;
        public bool HasAttachment;
    }


    public static void LoadMapObjects(BeatmapDifficulty beatmapDifficulty, out List<Note> notes, out List<Bomb> bombs, out List<Chain> chains, out List<Arc> arcs, out List<Wall> walls)
    {
        // split arcs into heads and tails for easier processing
        List<BeatmapSliderHead> beatmapSliderHeads = new List<BeatmapSliderHead>();
        List<BeatmapSliderTail> beatmapSliderTails = new List<BeatmapSliderTail>();
        for(int i = 0; i < beatmapDifficulty.sliders.Length; i++)
        {
            BeatmapSlider a = beatmapDifficulty.sliders[i];
            BeatmapSliderHead head = new BeatmapSliderHead();
            head.id = i;
            head.b = a.b;
            head.x = a.x;
            head.y = a.y;
            head.c = a.c;
            head.d = a.d;
            head.mu = a.mu;
            head.m = a.m;
            head.customData = a.customData;
            beatmapSliderHeads.Add(head);


            BeatmapSliderTail tail = new BeatmapSliderTail();
            tail.id = i;
            tail.b = a.tb;
            tail.x = a.tx;
            tail.y = a.ty;
            tail.d = a.tc;
            tail.mu = a.tmu;
            beatmapSliderTails.Add(tail);
        }

        List<BeatmapObject> allObjects = new List<BeatmapObject>();
        allObjects.AddRange(beatmapDifficulty.colorNotes);
        allObjects.AddRange(beatmapDifficulty.bombNotes);
        allObjects.AddRange(beatmapDifficulty.burstSliders);
        allObjects.AddRange(beatmapDifficulty.obstacles);
        allObjects.AddRange(beatmapSliderHeads);
        allObjects.AddRange(beatmapSliderTails);
        allObjects = allObjects.OrderBy(x => x.b).ToList();

        notes = new List<Note>();
        bombs = new List<Bomb>();
        chains = new List<Chain>();
        arcs = new List<Arc>();
        walls = new List<Wall>();

        List<BeatmapObject> sameBeatObjects = new List<BeatmapObject>();
        for(int i = 0; i < allObjects.Count; i++)
        {
            BeatmapObject current = allObjects[i];

            if(sameBeatObjects.Count == 0 || !CheckSameBeat(current.b, sameBeatObjects[0].b))
            {
                //This object doesn't share the same beat with the previous objects
                sameBeatObjects.Clear();

                for(int x = i; x < allObjects.Count; x++)
                {
                    //Gather all consecutive objects that share the same beat
                    BeatmapObject check = allObjects[x];
                    if(CheckSameBeat(check.b, current.b))
                    {
                        sameBeatObjects.Add(check);
                        //Skip to the first object that doesn't share this beat next loop
                        i = x;
                    }
                    else break;
                }
            }

            //Precalculate values for all objects on this beat
            List<BeatmapColorNote> notesOnBeat = sameBeatObjects.OfType<BeatmapColorNote>().ToList();
            List<BeatmapBombNote> bombsOnBeat = sameBeatObjects.OfType<BeatmapBombNote>().ToList();
            List<BeatmapBurstSlider> burstSlidersOnBeat = sameBeatObjects.OfType<BeatmapBurstSlider>().ToList();
            List<BeatmapObstacle> obstaclesOnBeat = sameBeatObjects.OfType<BeatmapObstacle>().ToList();
            List<BeatmapSliderHead> sliderHeadsOnBeat = sameBeatObjects.OfType<BeatmapSliderHead>().ToList();
            List<BeatmapSliderTail> sliderTailsOnBeat = sameBeatObjects.OfType<BeatmapSliderTail>().ToList();

            List<BeatmapObject> notesAndBombs = sameBeatObjects.Where(x => (x is BeatmapColorNote) || (x is BeatmapBombNote)).ToList();

            foreach(BeatmapColorNote n in notesOnBeat)
            {
                Note newNote = Note.NoteFromBeatmapColorNote(n);
                newNote.StartY = NoteManager.GetStartY(n, notesAndBombs);
                newNote.IsChainHead = NoteManager.CheckChainHead(n, burstSlidersOnBeat);

                // set angle snapping here because angle offset is an int in ColorNote
                newNote.Angle = NoteManager.GetAngleSnap(n, notesOnBeat) ?? newNote.Angle;

                // check attachment to arcs
                foreach(BeatmapSliderHead a in sliderHeadsOnBeat)
                {
                    if(!a.HasAttachment && a.x == n.x && n.y == a.y)
                    {
                        a.StartY = newNote.StartY;
                        a.HasAttachment = true;
                    }
                }

                foreach(BeatmapSliderTail a in sliderTailsOnBeat)
                {
                    if(!a.HasAttachment && a.x == n.x && n.y == a.y)
                    {
                        a.StartY = newNote.StartY;
                        a.HasAttachment = true;
                    }
                }

                notes.Add(newNote);
            }

            foreach(BeatmapBombNote b in bombsOnBeat)
            {
                Bomb newBomb = Bomb.BombFromBeatmapBombNote(b);
                newBomb.StartY = NoteManager.GetStartY(b, notesAndBombs);

                // check attachment to arcs
                foreach(BeatmapSliderHead a in sliderHeadsOnBeat)
                {
                    if(!a.HasAttachment && a.x == b.x && b.y == a.y)
                    {
                        a.StartY = newBomb.StartY;
                        a.HasAttachment = true;
                    }
                }

                foreach(BeatmapSliderTail a in sliderTailsOnBeat)
                {
                    if(!a.HasAttachment && a.x == b.x && b.y == a.y)
                    {
                        a.StartY = newBomb.StartY;
                        a.HasAttachment = true;
                    }
                }

                bombs.Add(newBomb);
            }

            foreach(BeatmapBurstSlider b in burstSlidersOnBeat)
            {
                Chain newChain = Chain.ChainFromBeatmapBurstSlider(b);
                chains.Add(newChain);
            }

            foreach(BeatmapObstacle o in obstaclesOnBeat)
            {
                Wall newWall = Wall.WallFromBeatmapObstacle(o);
                walls.Add(newWall);
            }
        }

        // pair slider heads/tails back up and make final arcs
        beatmapSliderHeads = beatmapSliderHeads.OrderBy(a => a.id).ToList();
        beatmapSliderTails = beatmapSliderTails.OrderBy(a => a.id).ToList();
        for(int i = 0; i < beatmapSliderHeads.Count; i++)
        {
            BeatmapSliderHead head = beatmapSliderHeads[i];
            BeatmapSliderTail tail = beatmapSliderTails[i];

            Arc newArc = Arc.ArcFromBeatmapSlider(beatmapDifficulty.sliders[i]);

            const float halfNoteOffset = 0.225f;
            if(head.HasAttachment)
            {
                Vector2 offset = DirectionVector(CalculateObjectAngle(head.d)) * halfNoteOffset;
                newArc.Position += offset;
                newArc.HeadControlPoint += offset;
                newArc.HeadStartY = head.StartY + offset.y;
            }

            if(tail.HasAttachment)
            {
                Vector2 offset = DirectionVector(CalculateObjectAngle(tail.d)) * halfNoteOffset * -1;
                newArc.TailPosition += offset;
                newArc.TailControlPoint += offset;
                newArc.TailStartY = tail.StartY + offset.y;
            }

            arcs.Add(newArc);
        }
    }


    private void Awake()
    {
        if(Instance && Instance != this)
        {
            Debug.Log("Duplicate ObjectManager in scene.");
            this.enabled = false;
        }
        else Instance = this;
    }


    private void Start()
    {
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateManagers;
    }


    private void OnDisable()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }
}


public abstract class MapObject
{
    public float Beat;
    public GameObject Visual;
    public Vector2 Position;
}


public abstract class HitSoundEmitter : MapObject
{
    public AudioSource source;
}


public abstract class BaseSlider : MapObject
{
    public int Color;
    public float TailBeat;
    public Vector2 TailPosition;
}