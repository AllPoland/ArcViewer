using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NoteManager : MonoBehaviour
{
    [Header("Object Pools")]
    [SerializeField] private ObjectPool notePool;
    [SerializeField] private ObjectPool bombPool;

    [Header("Object Parents")]
    [SerializeField] private GameObject noteParent;
    [SerializeField] private GameObject bombParent;

    [Header("Meshes")]
    [SerializeField] private Mesh noteMesh;
    [SerializeField] private Mesh chainHeadMesh;

    [Header("Materials")]
    [SerializeField] private Material complexMaterialRed;
    [SerializeField] private Material complexMaterialBlue;
    [SerializeField] private Material simpleMaterialRed;
    [SerializeField] private Material simpleMaterialBlue;
    [SerializeField] private Material arrowMaterialRed;
    [SerializeField] private Material arrowMaterialBlue;

    [Header("References")]
    [SerializeField] private ChainManager chainManager;

    public List<Note> Notes = new List<Note>();
    public List<Note> RenderedNotes = new List<Note>();

    public List<Bomb> Bombs = new List<Bomb>();
    public List<Bomb> RenderedBombs = new List<Bomb>();

    private ObjectManager objectManager;




    public void ReloadNotes()
    {
        ClearRenderedNotes();
        notePool.SetPoolSize(40);
        bombPool.SetPoolSize(40);

        UpdateNoteVisuals(TimeManager.CurrentBeat);
    }


    public void UpdateNoteVisual(Note n)
    {
        //Calculate the Z position based on time
        float noteTime = TimeManager.TimeFromBeat(n.Beat);

        float reactionTime = BeatmapManager.ReactionTime;

        float worldDist = objectManager.GetZPosition(noteTime);
        Vector3 worldPos = new Vector3(n.Position.x, n.Position.y, worldDist);

        if(objectManager.doMovementAnimation)
        {
            worldPos.y = objectManager.GetObjectY(n.StartY, worldPos.y, noteTime);
        }

        float angle = n.Angle;

        float jumpTime = TimeManager.CurrentTime + reactionTime;
        float jumpProgress = (jumpTime - noteTime) / reactionTime;

        if (objectManager.doFlipAnimation)
        {
            if(jumpProgress <= 0)
            {
                worldPos.x = n.FlipStartX;
            }
            else if(jumpProgress < 0.5f)
            {
                worldPos.x = Mathf.Lerp(n.FlipStartX, n.Position.x, Easings.Quad.InOut(jumpProgress / 0.5f));
                worldPos.y += n.FlipYHeight * (0.5f - Mathf.Cos(jumpProgress * Mathf.PI * 4) / 2);
            }
        }

        if(objectManager.doRotationAnimation)
        {
            if(jumpProgress <= 0)
            {
                //Note is still jumping in
                angle = 0;
            }
            else if(jumpProgress < objectManager.rotationAnimationTime)
            {
                float rotationProgress = jumpProgress / objectManager.rotationAnimationTime;
                float angleDist = Easings.Sine.Out(rotationProgress);

                angle *= angleDist;
            }
        }

        if(n.Visual == null)
        {
            n.Visual = notePool.GetObject();
            n.Visual.transform.SetParent(noteParent.transform);

            n.noteHandler = n.Visual.GetComponent<NoteHandler>();
            n.source = n.noteHandler.audioSource;

            n.noteHandler.SetMesh(n.IsChainHead ? chainHeadMesh : noteMesh);

            n.noteHandler.SetArrow(!n.IsDot);
            n.noteHandler.SetArrowMaterial(n.Color == 0 ? arrowMaterialRed : arrowMaterialBlue);

            if(objectManager.useSimpleNoteMaterial)
            {
                n.noteHandler.SetMaterial(n.Color == 0 ? simpleMaterialRed : simpleMaterialBlue);
            }
            else
            {
                n.noteHandler.SetMaterial(n.Color == 0 ? complexMaterialRed : complexMaterialBlue);
            }

            n.Visual.SetActive(true);
            n.noteHandler.EnableVisual();

            if(TimeManager.Playing && SettingsManager.GetFloat("hitsoundvolume") > 0)
            {
                HitSoundManager.ScheduleHitsound(noteTime, n.source);
            }

            RenderedNotes.Add(n);
        }

        n.Visual.transform.localPosition = worldPos;
        n.Visual.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }


    public void UpdateBombVisual(Bomb b)
    {
        float bombTime = TimeManager.TimeFromBeat(b.Beat);
        float worldDist = objectManager.GetZPosition(bombTime);

        Vector3 worldPos = new Vector3(b.Position.x, b.Position.y, worldDist);

        if(objectManager.doMovementAnimation)
        {
            worldPos.y = objectManager.GetObjectY(b.StartY, worldPos.y, bombTime);
        }

        if(b.Visual == null)
        {
            b.Visual = bombPool.GetObject();
            b.Visual.transform.SetParent(bombParent.transform);
            b.Visual.SetActive(true);

            RenderedBombs.Add(b);
        }
        b.Visual.transform.localPosition = worldPos;
    }


    private void ReleaseNote(Note n)
    {
        n.source.Stop();
        notePool.ReleaseObject(n.Visual);

        n.Visual = null;
        n.source = null;
        n.noteHandler = null;
    }


    private void ReleaseBomb(Bomb b)
    {
        bombPool.ReleaseObject(b.Visual);
        b.Visual = null;
    }


    public void ClearOutsideNotes()
    {
        if(RenderedNotes.Count > 0)
        {
            for(int i = RenderedNotes.Count - 1; i >= 0; i--)
            {
                Note n = RenderedNotes[i];
                if(!objectManager.CheckInSpawnRange(n.Beat))
                {
                    if(n.source.isPlaying)
                    {
                        //Only clear the visual elements if the hitsound is still playing
                        n.noteHandler.DisableVisual();
                        continue;
                    }

                    ReleaseNote(n);
                    RenderedNotes.Remove(n);
                }
                else if(!n.noteHandler.Visible)
                {
                    n.noteHandler.EnableVisual();
                }
            }
        }

        if(RenderedBombs.Count > 0)
        {
            for(int i = RenderedBombs.Count - 1; i >= 0; i--)
            {
                Bomb b = RenderedBombs[i];
                if(!CheckBombInSpawnRange(b.Beat))
                {
                    ReleaseBomb(b);
                    RenderedBombs.Remove(b);
                }
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
                ReleaseNote(n);
            }
            RenderedNotes.Clear();
        }

        if(RenderedBombs.Count > 0)
        {
            foreach(Bomb b in RenderedBombs)
            {
                ReleaseBomb(b);
            }
            RenderedBombs.Clear();
        }
    }


    public void UpdateNoteVisuals(float beat)
    {
        ClearOutsideNotes();

        if(Notes.Count > 0)
        {
            int firstNote = Notes.FindIndex(x => objectManager.CheckInSpawnRange(x.Beat));
            if(firstNote >= 0)
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
            int firstBomb = Bombs.FindIndex(x => CheckBombInSpawnRange(x.Beat));
            if(firstBomb >= 0)
            {
                for(int i = firstBomb; i < Bombs.Count; i++)
                {
                    Bomb b = Bombs[i];
                    if(CheckBombInSpawnRange(b.Beat))
                    {
                        UpdateBombVisual(b);
                    }
                    else break;
                }
            }
        }
    }


    private bool CheckBombInSpawnRange(float beat)
    {
        float bombTime = TimeManager.TimeFromBeat(beat);
        bool timeInRange = TimeManager.CurrentTime >= bombTime && TimeManager.CurrentTime <= bombTime - objectManager.BehindCameraTime;

        return objectManager.CheckInSpawnRange(beat) || timeInRange;
    }


    public void RescheduleHitsounds(bool playing)
    {
        if(!playing)
        {
            return;
        }

        foreach(Note n in RenderedNotes)
        {
            if(n.source != null && SettingsManager.GetFloat("hitsoundvolume") > 0)
            {
                HitSoundManager.ScheduleHitsound(TimeManager.TimeFromBeat(n.Beat), n.source);
            }
        }
    }


    public static int GetStartY(BeatmapObject n, List<BeatmapObject> sameBeatObjects)
    {
        List<BeatmapObject> objectsOnBeat = new List<BeatmapObject>(sameBeatObjects);

        if(n.y <= 0) return 0;

        if(objectsOnBeat.Count == 0) return 0;

        //Remove all notes that aren't directly below this one
        objectsOnBeat.RemoveAll(x => x.x != n.x || x.y >= n.y);

        if(objectsOnBeat.Count == 0) return 0;

        //Need to recursively calculate the startYs of each note underneath
        return (objectsOnBeat.Max(x => GetStartY(x, objectsOnBeat)) + 1);
    }


    public static bool CheckChainHead(BeatmapColorNote n, List<BeatmapBurstSlider> sameBeatBurstSliders)
    {
        foreach(BeatmapBurstSlider c in sameBeatBurstSliders)
        {
            if(c.x == n.x && c.y == n.y && c.c == n.c)
            {
                return true;
            }
        }

        return false;
    }


    public static (float?, float?) GetSnapAngles(List<BeatmapColorNote> sameBeatNotes)
    {
        List<BeatmapColorNote> redNotes = sameBeatNotes.Where(x => x.c == 0).ToList();
        List<BeatmapColorNote> blueNotes = sameBeatNotes.Where(x => x.c == 1).ToList();

        //Returns the angle the notes should use to snap, or null if they shouldn't
        float? redDesiredAngle = null;
        float? blueDesiredAngle = null;

        if(redNotes.Count == 2)
        {
            //Angle snapping requires exactly 2 notes
            BeatmapColorNote first = redNotes[0];
            BeatmapColorNote second = redNotes[1];

            redDesiredAngle = GetAngleSnap(first, second);
        }

        if(blueNotes.Count == 2)
        {
            //Angle snapping requires exactly 2 notes
            BeatmapColorNote first = blueNotes[0];
            BeatmapColorNote second = blueNotes[1];

            blueDesiredAngle = GetAngleSnap(first, second);
        }

        return (redDesiredAngle, blueDesiredAngle);
    }


    public static float? GetAngleSnap(BeatmapColorNote first, BeatmapColorNote second)
    {
        if(first.x == second.x || first.y == second.y)
        {
            //Don't snap notes that are on the same row or column
            return null;
        }

        if(!(first.d == second.d || first.d == 8 || second.d == 8))
        {
            //Notes must have the same direction
            return null;
        }

        Vector2 deltaPos = new Vector2(first.x - second.x, first.y - second.y);

        float firstAngle = ObjectManager.CalculateObjectAngle(first.d);
        float secondAngle = ObjectManager.CalculateObjectAngle(second.d);
        float desiredAngle = Mathf.Atan2(-deltaPos.x, deltaPos.y) * Mathf.Rad2Deg;

        if((first.d != 8 && Mathf.Abs(Mathf.DeltaAngle(desiredAngle, firstAngle)) > 90) || (second.d != 8 && Mathf.Abs(Mathf.DeltaAngle(desiredAngle, secondAngle)) > 90))
        {
            desiredAngle = Mathf.Atan2(deltaPos.x, -deltaPos.y) * Mathf.Rad2Deg;
        }

        if((first.d != 8 && Mathf.Abs(Mathf.DeltaAngle(desiredAngle, firstAngle)) > 40) || (second.d != 8 && Mathf.Abs(Mathf.DeltaAngle(desiredAngle, secondAngle)) > 40))
        {
            return null;
        }

        return desiredAngle;
    }


    public static (float, float) GetFlipYHeights(BeatmapColorNote first, BeatmapColorNote second)
    {
        const float flipYHeightOver = 0.45f;
        const float flipYHeightUnder = -0.15f;

        if(first.c == second.c)
        {
            return (0f, 0f);
        }

        if((first.c == 0 && first.x <= second.x) || (first.c == 1 && first.x >= second.x))
        {
            // non-crossover double
            return (0f, 0f);
        }

        if(first.c == 0)
        {
            if(first.y < second.y)
            {
                return (flipYHeightUnder, flipYHeightOver);
            }
            else
            {
                return (flipYHeightOver, flipYHeightUnder);
            }
        }
        else
        {
            if(first.y > second.y)
            {
                return (flipYHeightOver, flipYHeightUnder);
            }
            else
            {
                return (flipYHeightUnder, flipYHeightOver);
            }
        }
    }


    private void Start()
    {
        objectManager = ObjectManager.Instance;

        TimeManager.OnPlayingChanged += RescheduleHitsounds;
    }
}


public class Note : HitSoundEmitter
{
    public int Color;
    public float Angle;
    public float StartY;
    public float FlipStartX;
    public float FlipYHeight;
    public bool IsDot;
    public bool IsChainHead;
    public NoteHandler noteHandler;


    public static Note NoteFromBeatmapColorNote(BeatmapColorNote n)
    {
        Vector2 position = ObjectManager.CalculateObjectPosition(n.x, n.y, n.customData?.coordinates);
        float angle = ObjectManager.CalculateObjectAngle(n.d, n.a);

        return new Note
        {
            Beat = n.b,
            Position = position,
            Color = n.c,
            Angle = n.customData?.angle ?? angle,
            FlipStartX = position.x,
            IsDot = n.d == 8
        };
    }
}


public class Bomb : MapObject
{
    public float StartY;

    public static Bomb BombFromBeatmapBombNote(BeatmapBombNote b)
    {
        Vector2 position = ObjectManager.CalculateObjectPosition(b.x, b.y, b.customData?.coordinates);

        return new Bomb
        {
            Beat = b.b,
            Position = position
        };
    }
}