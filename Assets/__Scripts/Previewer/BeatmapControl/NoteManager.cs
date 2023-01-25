using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NoteManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject[] redNotePrefabs;
    [SerializeField] private GameObject[] blueNotePrefabs;
    [SerializeField] private GameObject bombPrefab;

    [Header("Object Parents")]
    [SerializeField] private GameObject noteParent;
    [SerializeField] private GameObject bombParent;

    [SerializeField] private float floorOffset;

    public List<Note> Notes = new List<Note>();
    public List<Note> RenderedNotes = new List<Note>();

    public List<Bomb> Bombs = new List<Bomb>();
    public List<Bomb> RenderedBombs = new List<Bomb>();

    private ObjectManager objectManager;

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


    public void LoadNotesFromDifficulty(Difficulty difficulty)
    {
        ClearRenderedNotes();
        BeatmapDifficulty beatmap = difficulty.beatmapDifficulty;

        if(beatmap.colorNotes.Length > 0)
        {
            List<Note> newNoteList = new List<Note>();
            foreach(ColorNote n in beatmap.colorNotes)
            {
                Note newNote = Note.NoteFromColorNote(n);
                //Preprocess window snap and start position because objectManager.GetObjectsOnBeat() has big overhead
                newNote.WindowSnap = CheckAngleSnap(newNote);
                newNote.StartY = GetStartY<Note>(newNote);

                newNoteList.Add(newNote);
            }
            Notes = ObjectManager.SortObjectsByBeat<Note>(newNoteList);
        }
        else
        {
            Notes = new List<Note>();
        }

        if(beatmap.bombNotes.Length > 0)
        {
            List<Bomb> newBombList = new List<Bomb>();
            foreach(BombNote b in beatmap.bombNotes)
            {
                Bomb newBomb = Bomb.BombFromBombNote(b);
                newBomb.StartY = GetStartY<Bomb>(newBomb);

                newBombList.Add(newBomb);
            }
            Bombs = ObjectManager.SortObjectsByBeat<Bomb>(newBombList);
        }
        else
        {
            Bombs = new List<Bomb>();
        }

        UpdateNoteVisuals(TimeManager.CurrentBeat);
    }


    private bool CheckSameDirection(Note n, Note other)
    {
        return n.Direction == other.Direction || n.Direction == 8 || other.Direction == 8;
    }


    private bool CheckDiagonalNote(Note n)
    {
        return n.Direction == 8 || DirectionAngles[n.Direction] % 90 != 0;
    }


    private bool CheckUpwardNote(Note n)
    {
        return n.Direction == 8 || Mathf.Abs(DirectionAngles[n.Direction]) > 90;
    }


    private bool CheckRightNote(Note n)
    {
        return n.Direction == 8 || DirectionAngles[n.Direction] > 0;
    }


    private float GetWindowAngleOrthogonal(WindowType type)
    {
        const float knightMoveAngle = 26.565f;
        const float fourWideAngle = 18.435f;
        const float maxWideAngle = 33.69f;

        switch(type)
        {
            case WindowType.None: return 0;
            case WindowType.KnightMove: return knightMoveAngle;
            case WindowType.FourWide: return fourWideAngle;
            case WindowType.MegaWide: return maxWideAngle;
            default: return 0;
        }
    }


    private float GetWindowAngleDiagonal(WindowType type)
    {
        const float knightMoveAngleDiagonal = 18.435f;
        const float fourWideAngleDiagonal = 26.565f;
        const float maxWideAngleDiagonal = 11.31f;

        switch(type)
        {
            case WindowType.None: return 0;
            case WindowType.KnightMove: return knightMoveAngleDiagonal;
            case WindowType.FourWide: return fourWideAngleDiagonal;
            case WindowType.MegaWide: return maxWideAngleDiagonal;
            default: return 0;
        }
    }


    private float CheckAngleSnap(Note n)
    {
        //Returns the angle offset the note should use to snap, or 0 if it shouldn't
        List<Note> notesOnBeat = ObjectManager.GetObjectsOnBeat<Note>(Notes, n.Beat);
        if(notesOnBeat.Count == 0) return 0;

        notesOnBeat.RemoveAll(x => x.Color != n.Color);

        if(notesOnBeat.Count != 2)
        {
            //Angle snapping requires exactly 2 notes
            return 0;
        }

        //Disregard notes that are on the same row or column
        notesOnBeat.RemoveAll(x => x.x == n.x || x.y == n.y);
        if(notesOnBeat.Count == 0)
        {
            return 0;
        }

        Note other = notesOnBeat[0];
        bool otherDot = other.Direction == 8;

        if(n.x == other.x || n.y == other.y)
        {
            //Straight horizontal or vertical angle doesn't need snapping
            return 0;
        }
        if(!CheckSameDirection(n, other))
        {
            //Notes must have the same direction
            return 0;
        }

        int xDist = (int)(n.x - other.x);
        int yDist = (int)(n.y - other.y);
        int absxDist = Mathf.Abs(xDist);
        int absyDist = Mathf.Abs(yDist);

        bool dot = n.Direction == 8;
        bool counterClockwise = xDist >= 0 != yDist >= 0;
        int directionMult = counterClockwise ? 1 : -1;

        bool equalDistance = absxDist == absyDist;
        if(equalDistance)
        {
            //Equal distances always means a 45 degree angle
            if(!dot)
            {
                //No snapping needed for 45 degrees
                return 0;
            }
            else
            {
                if(otherDot)
                {
                    //Dots snap to 45 degrees
                    return 45 * directionMult;
                }
                if(!CheckDiagonalNote(other))
                {
                    //Don't snap if the other note isn't diagonal
                    return 0;
                }
                if((CheckUpwardNote(other) == yDist < 0) == (CheckRightNote(other) == xDist < 0))
                {
                    //Arrow points towards this dot or directly away from this dot
                    return 45 * directionMult;
                }
            }
        }

        //Windows!!!
        WindowType windowType = WindowType.None;
        if((absxDist == 2 && absyDist == 1) || (absxDist == 1 && absyDist == 2))
        {
            windowType = WindowType.KnightMove;
        }
        else if(absxDist == 3 && absyDist == 1)
        {
            windowType = WindowType.FourWide;
        }
        else if(absxDist == 3 && absyDist == 2)
        {
            windowType = WindowType.MegaWide;
        }
        
        bool mainVertical = windowType == WindowType.KnightMove && absyDist == 2;

        float angle = 0;
        if(dot)
        {
            //Force dots to match rotation direction
            angle = DirectionAngles[other.Direction];
        }

        if(mainVertical)
        {
            if(n.Direction == 0 || n.Direction == 1 || (dot && (other.Direction == 0 || other.Direction == 1 || otherDot)))
            {
                //Vertical notes should snap
                if(dot && !otherDot) angle *= directionMult;
                angle += GetWindowAngleOrthogonal(windowType);

                return angle * directionMult;
            }
            directionMult *= -1;
        }
        else if(n.Direction == 2 || n.Direction == 3 || (dot && (other.Direction == 2 || other.Direction == 3 || otherDot)))
        {
            //Horizontal notes should snap
            if(dot && !otherDot) angle *= directionMult * -1;
            angle += GetWindowAngleOrthogonal(windowType);

            return angle * directionMult * -1;
        }

        if(CheckDiagonalNote(n) && CheckDiagonalNote(other))
        {
            bool up = yDist > 0;
            bool right = xDist > 0;
            bool correctDiagonal = (CheckUpwardNote(n) == up) == (CheckRightNote(n) == right) || dot;
            bool otherCorrectDiagonal = (CheckUpwardNote(other) == up) == (CheckRightNote(other) == right) || otherDot;

            if(correctDiagonal && otherCorrectDiagonal)
            {
                if(dot && !otherDot) angle *= directionMult;
                angle += GetWindowAngleDiagonal(windowType);

                return angle * directionMult;
            }
        }

        return 0;
    }


    private static float SpawnParabola(float targetHeight, float baseHeight, float halfJumpDistance, float t)
    {
        float dSquared = Mathf.Pow(halfJumpDistance, 2);
        float tSquared = Mathf.Pow(t, 2);

        float movementRange = targetHeight - baseHeight;

        return -(movementRange / dSquared) * tSquared + targetHeight;
    }


    private int GetStartY<T>(T n) where T : BeatmapObject
    {
        if(n.y <= 0) return 0;

        List<Note> otherNotes = ObjectManager.GetObjectsOnBeat<Note>(Notes, n.Beat);
        List<Bomb> otherBombs = ObjectManager.GetObjectsOnBeat<Bomb>(Bombs, n.Beat);

        if(otherNotes.Count == 0 && otherBombs.Count == 0) return 0;

        //Remove all notes that aren't directly below this one
        otherNotes.RemoveAll(x => x.x != n.x || x.y >= n.y);
        otherBombs.RemoveAll(x => x.x != n.x || x.y >= n.y);

        if(otherNotes.Count == 0 && otherBombs.Count == 0) return 0;

        int startPos = 0;
        if(otherNotes.Count > 0)
        {
            //Need to recursively calculate the startYs of each note underneath
            int maxY = otherNotes.Max(x => GetStartY(x)) + 1;
            startPos = maxY;
        }
        if(otherBombs.Count > 0)
        {
            int maxY = otherBombs.Max(x => GetStartY(x)) + 1;
            startPos = Mathf.Max(startPos, maxY);
        }

        return startPos;
    }


    private float GetObjectY(float startY, float targetY, float objectTime)
    {
        float jumpTime = TimeManager.CurrentTime + BeatmapManager.ReactionTime;

        if(objectTime > jumpTime)
        {
            return startY;
        }
        
        float halfJumpDistance = BeatmapManager.JumpDistance / 2;
        return SpawnParabola(targetY, startY, halfJumpDistance, objectManager.GetZPosition(objectTime));
    }


    public void UpdateNoteVisual(Note n)
    {
        //Calculate the 2d position on the grid
        Vector2 gridPos = objectManager.bottomLeft;
        gridPos.x += n.x * objectManager.laneWidth;
        gridPos.y += n.y * objectManager.rowHeight;

        //Calculate the Z position based on time
        float noteTime = TimeManager.TimeFromBeat(n.Beat);

        float reactionTime = BeatmapManager.ReactionTime;
        float jumpTime = TimeManager.CurrentTime + reactionTime;

        float worldDist = objectManager.GetZPosition(noteTime);

        Vector3 targetPos = new Vector3(gridPos.x, gridPos.y, worldDist);

        //Default to 0 in case of over-sized direction value
        int directionIndex = n.Direction >= 0 && n.Direction < 9 ? n.Direction : 0;

        float snapAngle = n.WindowSnap;
        bool useSnap = snapAngle != 0;
        float adjustment = useSnap ? snapAngle : n.AngleOffset;

        float targetAngle = DirectionAngles[directionIndex] + adjustment;

        Vector3 worldPos = targetPos;
        float angle = targetAngle;

        float rotationAnimationLength = reactionTime * objectManager.rotationAnimationTime;

        float startY = n.StartY * objectManager.rowHeight + floorOffset;
        worldPos.y = GetObjectY(startY, targetPos.y, noteTime);

        if(noteTime > jumpTime)
        {
            //Note is still jumping in
            angle = 0;
        }
        else if(noteTime > jumpTime - rotationAnimationLength)
        {
            float timeSinceJump = reactionTime - (noteTime - TimeManager.CurrentTime);
            float rotationProgress = timeSinceJump / rotationAnimationLength;
            float angleDist = Easings.Sine.Out(rotationProgress);

            angle = targetAngle * angleDist;
        }

        if(n.Visual == null)
        {
            int dot = n.Direction == 8 ? 1 : 0;
            GameObject prefab = n.Color == 0 ? redNotePrefabs[dot] : blueNotePrefabs[dot];
            n.Visual = Instantiate(prefab);
            n.Visual.transform.SetParent(noteParent.transform);

            RenderedNotes.Add(n);
        }
        n.Visual.transform.localPosition = worldPos;
        n.Visual.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }


    public void UpdateBombVisual(Bomb b)
    {
        //Calculate the 2d position on the grid
        Vector2 gridPos = objectManager.bottomLeft;
        gridPos.x += b.x * objectManager.laneWidth;
        gridPos.y += b.y * objectManager.rowHeight;


        float bombTime = TimeManager.TimeFromBeat(b.Beat);
        float reactionTime = BeatmapManager.ReactionTime;
        float jumpTime = TimeManager.CurrentTime + reactionTime;

        float worldDist = objectManager.GetZPosition(bombTime);

        Vector3 targetPos = new Vector3(gridPos.x, gridPos.y, worldDist);

        Vector3 worldPos = targetPos;
        float startY = b.StartY * objectManager.rowHeight + floorOffset;
        worldPos.y = GetObjectY(startY, targetPos.y, bombTime);

        if(b.Visual == null)
        {
            b.Visual = Instantiate(bombPrefab);
            b.Visual.transform.SetParent(bombParent.transform);

            RenderedBombs.Add(b);
        }
        b.Visual.transform.localPosition = worldPos;
    }


    public void ClearOutsideNotes()
    {
        if(RenderedNotes.Count > 0)
        {
            //Holds a list of each note to be removed
            //This needs to happen since removing an item from a list we're looping through breaks things
            List<Note> removeNotes = new List<Note>();
            foreach(Note n in RenderedNotes)
            {
                if(!objectManager.CheckInSpawnRange(n.Beat))
                {
                    n.ClearVisual();
                    removeNotes.Add(n);
                }
            }

            //Actually remove the notes from the list now
            foreach(Note n in removeNotes)
            {
                RenderedNotes.Remove(n);
            }
        }

        if(RenderedBombs.Count > 0)
        {
            //Repeat above for bombs
            List<Bomb> removeBombs = new List<Bomb>();
            foreach(Bomb b in RenderedBombs)
            {
                if(!CheckBombInSpawnRange(b))
                {
                    b.ClearVisual();
                    removeBombs.Add(b);
                }
            }

            foreach(Bomb b in removeBombs)
            {
                RenderedBombs.Remove(b);
            }
        }
    }


    public void ClearRenderedNotes()
    {
        //Clear all rendered notes
        if(RenderedNotes.Count > 0)
        {
            foreach(Note n in RenderedNotes)
            {
                n.ClearVisual();
            }
            RenderedNotes.Clear();
        }

        if(RenderedBombs.Count > 0)
        {
            foreach(Bomb b in RenderedBombs)
            {
                b.ClearVisual();
            }
            RenderedBombs.Clear();
        }
    }


    public void UpdateNoteVisuals(float beat)
    {
        ClearOutsideNotes();

        if(Notes.Count > 0)
        {
            int firstNote = Notes.FindIndex(x => x.Beat > TimeManager.CurrentBeat);
            if(firstNote < 0)
            {
                //Debug.Log("No more notes.");
            }
            else
            {
                for(int i = firstNote; i < Notes.Count; i++)
                {
                    //Update each note's position
                    Note n = Notes[i];
                    if(objectManager.CheckInSpawnRange(n.Beat))
                    {
                        UpdateNoteVisual(n);
                    }
                    else break;
                }
            }
        }

        if(Bombs.Count > 0)
        {
            int firstBomb = Bombs.FindIndex(x => x.Beat > TimeManager.CurrentBeat + TimeManager.BeatFromTime(objectManager.BehindCameraTime));
            if(firstBomb < 0)
            {
                //Debug.Log("No more bombs.");
            }
            else
            {
                for(int i = firstBomb; i < Bombs.Count; i++)
                {
                    Bomb b = Bombs[i];
                    if(CheckBombInSpawnRange(b))
                    {
                        UpdateBombVisual(b);
                    }
                    else break;
                }
            }
        }
    }


    private bool CheckBombInSpawnRange(Bomb b)
    {
        float bombTime = TimeManager.TimeFromBeat(b.Beat);
        bool timeInRange = TimeManager.CurrentTime > bombTime && TimeManager.CurrentTime <= bombTime - objectManager.BehindCameraTime;

        return objectManager.CheckInSpawnRange(b.Beat) || timeInRange;
    }


    private void Start()
    {
        objectManager = ObjectManager.Instance;

        BeatmapManager.OnBeatmapDifficultyChanged += LoadNotesFromDifficulty;
        TimeManager.OnBeatChanged += UpdateNoteVisuals;
    }
}


enum WindowType
{
    None,
    KnightMove,
    FourWide,
    MegaWide
}