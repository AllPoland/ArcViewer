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
        
        float halfJumpDistance = BeatmapManager.JumpDistance / 2;
        return SpawnParabola(targetY, startY, halfJumpDistance, GetZPosition(objectTime));
    }


    private void OnEnable()
    {
        if(Instance && Instance != this)
        {
            Debug.Log("Duplicate ObjectManager in scene.");
            this.enabled = false;
        }
        else Instance = this;
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
    public float tailY;
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
            tailY = b.ty,
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