using System.Collections;
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
    [SerializeField] public float spawnAnimationTime = 0.1f;
    [SerializeField] public float behindCameraZ = -5f;

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


    public static List<T> GetObjectsOnBeat<T>(List<T> objects, float beat) where T :BeatmapObject
    {
        return objects.FindAll(x => x.Beat == beat);
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


    public float GetZPosition(float objectTime)
    {
        float reactionTime = BeatmapManager.ReactionTime;
        float jumpTime = TimeManager.CurrentTime + reactionTime;

        if(objectTime <= jumpTime)
        {
            //Note has jumped in. Place based on Jump Setting stuff
            float timeDist = (objectTime - TimeManager.CurrentTime);
            return WorldSpaceFromTime(timeDist);
        }
        else
        {
            //Note hasn't jumped in yet. Place based on the jump-in stuff
            float timeDist = (objectTime - TimeManager.CurrentTime - reactionTime) / moveTime;
            timeDist = Easings.Sine.Out(timeDist);
            return BeatmapManager.JumpDistance + (moveZ * timeDist);
        }
    }


    public float WorldSpaceFromTime(float time)
    {
        float NJS = BeatmapManager.CurrentMap.NoteJumpSpeed;
        return time * NJS / BeatmapManager.ReactionTime;
    }


    public float TimeFromWorldspace(float position)
    {
        float NJS = BeatmapManager.CurrentMap.NoteJumpSpeed;
        return (position / NJS) * BeatmapManager.ReactionTime;
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


    public void ClearVisual()
    {
        if(Visual == null) return;

        GameObject.Destroy(Visual);
        Visual = null;
    }
}


public class Note : BeatmapObject
{
    public int x;
    public int y;
    public int Color;
    public int Direction;
    public int AngleOffset;


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
    public int x;
    public int y;


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
    public int x;
    public int y;
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