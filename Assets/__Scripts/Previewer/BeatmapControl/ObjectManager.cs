using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    public static ObjectManager Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] public Vector2 bottomLeft = new Vector2(-0.9f, 0);
    [SerializeField] public float laneWidth = 0.6f;
    [SerializeField] public float rowHeight = 0.55f;

    [Header("Animation Settings")]
    [SerializeField] public float moveZ = 200f;
    [SerializeField] public float moveTime = 0.25f;
    [SerializeField] public float rotationAnimationTime = 0.3f;
    [SerializeField] public float behindCameraZ = -5f;
    [SerializeField] public float objectFloorOffset;

    [Header("Managers")]
    [SerializeField] private NoteManager noteManager;
    [SerializeField] private WallManager wallManager;
    [SerializeField] private ChainManager chainManager;
    [SerializeField] private ArcManager arcManager;

    public bool useSimpleNoteMaterial = false;
    public bool doRotationAnimation = true;
    public bool doMovementAnimation = true;


    public float BehindCameraTime
    {
        get
        {
            return TimeFromWorldspace(behindCameraZ);
        }
    }


    public static List<T> SortObjectsByBeat<T>(List<T> objects) where T : BeatmapObject
    {
        return objects.OrderBy(x => x.Beat).ToList();
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


    public static List<T> GetObjectsOnBeat<T>(List<T> search, float beat) where T: BeatmapObject
    {
        const float leeway = 0.001f;
        float time = TimeManager.TimeFromBeat(beat);

        return search.FindAll(x => Mathf.Abs(TimeManager.TimeFromBeat(x.Beat) - time) <= leeway);
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


    public static int GetStartY(BeatmapObject n, List<BeatmapObject>sameBeatObjects)
    {
        List<BeatmapObject> objectsOnBeat = new List<BeatmapObject>(sameBeatObjects);

        if(n.y <= 0) return 0;

        if(objectsOnBeat.Count == 0) return 0;

        //Remove all notes that aren't directly below this one
        objectsOnBeat.RemoveAll(x => x.x != n.x || x.y >= n.y);

        if(objectsOnBeat.Count == 0) return 0;

        //Need to recursively calculate the startYs of each note underneath
        return objectsOnBeat.Max(x => GetStartY(x, objectsOnBeat)) + 1;
    }


    public void UpdateManagers(Difficulty difficulty)
    {
        HitSoundManager.ClearScheduledSounds();

        //This exists to control the order in which each object type is created as opposed to whenever the event delegate wants it to
        //This shouldn't matter, but it does and I don't know how to fix it :smil:
        chainManager.LoadChainsFromDifficulty(difficulty);
        noteManager.LoadNotesFromDifficulty(difficulty);
        arcManager.LoadArcsFromDifficulty(difficulty);
        wallManager.LoadWallsFromDifficulty(difficulty);
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


public class BeatmapObject
{
    public float Beat;
    public GameObject Visual;
    public float x;
    public float y;
}


public class HitSoundEmitter : BeatmapObject
{
    public AudioSource source;
}


public class Note : HitSoundEmitter
{
    public int Color;
    public int Direction;
    public int AngleOffset;
    public float WindowSnap;
    public int StartY;
    public bool IsHead;
    public NoteHandler noteHandler;


    public static Note NoteFromColorNote(ColorNote n)
    {
        return new Note
        {
            Beat = n.b,
            x = n.x,
            y = n.y,
            Color = n.c,
            Direction = n.d,
            AngleOffset = n.a,
            Visual = null
        };
    }
}


public class Bomb : BeatmapObject
{
    public int StartY;

    public static Bomb BombFromBombNote(BombNote b)
    {
        return new Bomb
        {
            Beat = b.b,
            x = b.x,
            y = b.y,
            Visual = null
        };
    }
}


public class Wall : BeatmapObject
{
    public float Duration;
    public int Width;
    public int Height;


    public static Wall WallFromObstacle(Obstacle o)
    {
        return new Wall
        {
            Beat = o.b,
            x = o.x,
            y = o.y,
            Duration = o.d,
            Width = o.w,
            Height = o.h,
            Visual = null
        };
    }
}


public class Chain : BeatmapObject
{
    public int Color;
    public int Direction;
    public float TailBeat;
    public float TailX;
    public float TailY;
    public int SegmentCount;
    public float Squish;


    public static Chain ChainFromBurstSlider(BurstSlider b)
    {
        return new Chain
        {
            Beat = b.b,
            x = b.x,
            y = b.y,
            Color = b.c,
            Direction = b.d,
            TailBeat = b.tb,
            TailX = b.tx,
            TailY = b.ty,
            SegmentCount = b.sc,
            Squish = b.s
        };
    }
}


public class ChainLink : HitSoundEmitter
{
    public int Color;
    public float Angle;
    public ChainLinkHandler chainLinkHandler;
}


public class Arc : BeatmapObject
{
    public int Color;
    public int Direction;
    public float Modifier;
    public float TailBeat;
    public float TailX;
    public float TailY;
    public int TailDirection;
    public float TailModifier;
    public int RotationDirection;

    public ArcHandler arcHandler;
    public bool HasHeadAttachment;
    public bool HasTailAttachment;
    public float StartY;
    public float TailStartY;
    public Vector3 HeadPos;
    public Vector3 HeadModPos;
    public Vector3 TailModPos;
    public Vector3 TailPos;
    public Vector3 CurrentHeadPos;
    public Vector3 CurrentTailPos;


    public static Arc ArcFromArcSlider(ArcSlider a)
    {
        return new Arc
        {
            Beat = a.b,
            x = a.x,
            y = a.y,
            Color = a.c,
            Direction = a.d,
            Modifier = a.mu,
            TailBeat = a.tb,
            TailX = a.tx,
            TailY = a.ty,
            TailDirection = a.tc,
            TailModifier = a.tmu,
            RotationDirection = a.m
        };
    }
}