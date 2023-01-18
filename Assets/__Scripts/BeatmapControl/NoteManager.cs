using System.Collections;
using System.Collections.Generic;
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

    public List<Note> Notes = new List<Note>();
    public List<Note> RenderedNotes = new List<Note>();

    public List<Bomb> Bombs = new List<Bomb>();
    public List<Bomb> RenderedBombs = new List<Bomb>();

    private TimeManager timeManager;
    private BeatmapManager beatmapManager;
    private ObjectManager objectManager;

    public static readonly List<float> DirectionAngles = new List<float>
    {
        180,
        0,
        -90,
        90,
        -135,
        135,
        -45,
        45,
        0
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
                newNoteList.Add( Note.NoteFromColorNote(n) );
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
                newBombList.Add( Bomb.BombFromBombNote(b) );
            }
            Bombs = ObjectManager.SortObjectsByBeat<Bomb>(newBombList);
        }
        else
        {
            Bombs = new List<Bomb>();
        }

        UpdateNoteVisuals(timeManager.CurrentBeat);
    }


    public void UpdateNoteVisual(Note n)
    {
        //Calculate the 2d position on the grid
        Vector2 gridPos = objectManager.bottomLeft;
        gridPos.x += n.x * objectManager.laneWidth;
        gridPos.y += n.y * objectManager.rowHeight;

        //Calculate the Z position based on time
        float noteTime = TimeManager.TimeFromBeat(n.Beat);
        float reactionTime = beatmapManager.ReactionTime;
        float animationTime = objectManager.spawnAnimationTime;
        float jumpTime = timeManager.CurrentTime + reactionTime;

        float worldDist = objectManager.GetZPosition(noteTime);

        Vector3 worldPos = new Vector3(gridPos.x, gridPos.y, worldDist);

        if(n.Visual == null)
        {
            int dot = n.Direction == 8 ? 1 : 0;
            GameObject prefab = n.Color == 0 ? redNotePrefabs[dot] : blueNotePrefabs[dot];
            n.Visual = Instantiate(prefab);
            n.Visual.transform.SetParent(noteParent.transform);

            RenderedNotes.Add(n);
        }
        n.Visual.transform.localPosition = worldPos;

        int directionIndex = n.Direction >= 0 && n.Direction < 9 ? n.Direction : 0;
        float targetAngle = DirectionAngles[directionIndex] + n.AngleOffset;
        float angle = 0;

        float animationOffset = objectManager.spawnAnimationOffset;
        float animationFinishTime = jumpTime - (animationTime * animationOffset);
        if(noteTime <= animationFinishTime)
        {
            angle = targetAngle;
        }
        else if(noteTime <= animationFinishTime + animationTime)
        {
            float angleDist = 1 - ((noteTime - animationFinishTime) / animationTime);
            angleDist = Easings.Sine.Out(angleDist);
            angle = targetAngle * angleDist;
        }

        n.Visual.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }


    public void UpdateBombVisual(Bomb b)
    {
        //Calculate the 2d position on the grid
        Vector2 gridPos = objectManager.bottomLeft;
        gridPos.x += b.x * objectManager.laneWidth;
        gridPos.y += b.y * objectManager.rowHeight;


        float bombTime = TimeManager.TimeFromBeat(b.Beat);
        float reactionTime = beatmapManager.ReactionTime;
        float jumpTime = timeManager.CurrentTime + reactionTime;

        float worldDist = objectManager.GetZPosition(bombTime);

        Vector3 worldPos = new Vector3(gridPos.x, gridPos.y, worldDist);

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
            int firstNote = Notes.FindIndex(x => x.Beat > timeManager.CurrentBeat);
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
            int firstBomb = Bombs.FindIndex(x => x.Beat > timeManager.CurrentBeat + TimeManager.BeatFromTime(objectManager.BehindCameraTime));
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
        bool timeInRange = timeManager.CurrentTime > bombTime && timeManager.CurrentTime <= bombTime - objectManager.BehindCameraTime;

        return objectManager.CheckInSpawnRange(b.Beat) || timeInRange;
    }


    private void Start()
    {
        timeManager = TimeManager.Instance;
        beatmapManager = BeatmapManager.Instance;
        objectManager = ObjectManager.Instance;

        if(beatmapManager != null)
        {
            beatmapManager.OnBeatmapDifficultyChanged += LoadNotesFromDifficulty;
        }
        
        if(timeManager != null)
        {
            timeManager.OnBeatChanged += UpdateNoteVisuals;
        }
    }
}