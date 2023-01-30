using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ArcManager : MonoBehaviour
{
    public static float ArcSegmentDensity = 30f;

    public List<Arc> Arcs = new List<Arc>();
    public List<Arc> RenderedArcs = new List<Arc>();

    [SerializeField] private ObjectPool arcPool;
    [SerializeField] private GameObject arcParent;

    [SerializeField] private Material redArcMaterial;
    [SerializeField] private Material blueArcMaterial;
    [SerializeField] private Material arcCenterMaterial;

    [SerializeField] private NoteManager noteManager;

    [SerializeField] private float arcFadeTransitionLength;
    [SerializeField] private float arcCenterFadeTransitionLength;
    [SerializeField] private float arcCloseFadeDist;

    private static ObjectManager objectManager;

    private Material currentRedArcMaterial;
    private Material currentBlueArcMaterial;
    private Material currentArcCenterMaterial;


    public void LoadArcsFromDifficulty(Difficulty difficulty)
    {
        ClearRenderedArcs();
        arcPool.SetPoolSize(20);

        Arcs.Clear();

        BeatmapDifficulty beatmap = difficulty.beatmapDifficulty;
        if(beatmap.sliders.Length > 0)
        {
            foreach(ArcSlider s in beatmap.sliders)
            {
                Arc newArc = Arc.ArcFromArcSlider(s);

                CheckArcAttachments(newArc, difficulty);
                GetArcControlPoints(newArc);

                Arcs.Add(newArc);
            }
            Arcs = ObjectManager.SortObjectsByBeat<Arc>(Arcs);
        }

        UpdateMaterials();
        UpdateArcVisuals(TimeManager.CurrentBeat);
    }


    public void UpdateMaterials()
    {
        //Sets the distance that arcs should fade out
        float fadeDist = BeatmapManager.JumpDistance / 2;
        float closeFadeDist = SettingsManager.GetFloat("cameraposition") + arcCloseFadeDist;

        currentRedArcMaterial = new Material(redArcMaterial);
        currentBlueArcMaterial = new Material(blueArcMaterial);
        currentArcCenterMaterial = new Material(arcCenterMaterial);

        //So many parameters :fw_nofwoompdespair:
        currentRedArcMaterial.SetFloat("_FadeStartPoint", closeFadeDist);
        currentRedArcMaterial.SetFloat("_FadeEndPoint", fadeDist);
        currentRedArcMaterial.SetFloat("_FadeTransitionLength", arcFadeTransitionLength);

        currentBlueArcMaterial.SetFloat("_FadeStartPoint", closeFadeDist);
        currentBlueArcMaterial.SetFloat("_FadeEndPoint", fadeDist);
        currentBlueArcMaterial.SetFloat("_FadeTransitionLength", arcFadeTransitionLength);

        currentArcCenterMaterial.SetFloat("_FadeStartPoint", closeFadeDist);
        currentArcCenterMaterial.SetFloat("_FadeEndPoint", fadeDist);
        currentArcCenterMaterial.SetFloat("_FadeTransitionLength", arcCenterFadeTransitionLength);
    }


    public void CheckArcAttachments(Arc a, Difficulty difficulty)
    {
        List<Note> notes = noteManager.Notes;
        Note attachedHeadNote = notes.FirstOrDefault(x => ObjectManager.CheckSameBeat(x.Beat, a.Beat) && x.x == a.x && x.y == a.y);
        if(attachedHeadNote != null)
        {
            a.HasHeadAttachment = true;
            a.StartY = attachedHeadNote.StartY;
        }

        Note attachedTailNote = notes.FirstOrDefault(x => ObjectManager.CheckSameBeat(x.Beat, a.TailBeat) && x.x == a.TailX && x.y == a.TailY);
        if(attachedTailNote != null)
        {
            a.HasTailAttachment = true;
            a.TailStartY = attachedTailNote.StartY;
        }

        List<Bomb> bombs = noteManager.Bombs;
        if(!a.HasHeadAttachment)
        {
            Bomb attachedBomb = bombs.FirstOrDefault(x => ObjectManager.CheckSameBeat(x.Beat, a.Beat) && x.x == a.x && x.y == a.y);
            if(attachedBomb != null)
            {
                a.HasHeadAttachment = true;
                a.StartY = attachedBomb.StartY;
            }
        }
        if(!a.HasTailAttachment)
        {
            Bomb attachedBomb = bombs.FirstOrDefault(x => ObjectManager.CheckSameBeat(x.Beat, a.TailBeat) && x.x == a.TailX && x.y == a.TailY);
            if(attachedBomb != null)
            {
                a.HasTailAttachment = true;
                a.TailStartY = attachedBomb.StartY;
            }
        }
    }


    public static void GetArcControlPoints(Arc a)
    {
        float startTime = TimeManager.TimeFromBeat(a.Beat);
        float endTime = TimeManager.TimeFromBeat(a.TailBeat);
        float duration = endTime - startTime;

        //Calculate the head and tail positions in worldspace
        Vector2 gridPos = objectManager.bottomLeft;

        float headX = a.x * objectManager.laneWidth;
        float headY = a.y * objectManager.rowHeight;
        Vector3 headPos = new Vector3(gridPos.x + headX, gridPos.y + headY, 0);

        float tailX = a.TailX * objectManager.laneWidth;
        float tailY = a.TailY * objectManager.rowHeight;
        float tailZ = objectManager.WorldSpaceFromTime(duration);
        Vector3 tailPos = new Vector3(gridPos.x + tailX, gridPos.y + tailY, tailZ);

        //Arcs should be offset to the edge of the note if the direction isn't a dot
        //This isn't 0.25??????? Beat Games moment
        const float halfCubeSize = 0.225f;

        int headDirection = Mathf.Clamp(a.Direction, 0, 8);
        Vector2 headFaceVector = headDirection == 8 ? Vector2.zero : ChainManager.noteVectorFromDirection[headDirection];
        a.HeadPos = headPos + ((Vector3)headFaceVector * halfCubeSize);

        int tailDirection = Mathf.Clamp(a.TailDirection, 0, 8);
        Vector2 tailFaceVector = ChainManager.noteVectorFromDirection[tailDirection];
        a.TailPos = tailPos + ((Vector3)tailFaceVector * -halfCubeSize); //Tail should move in the opposite directon the arrow points

        a.CurrentHeadPos = a.HeadPos;
        a.CurrentTailPos = a.TailPos;

        //Get p1 and p2 from the modifier values
        const float baseModifier = 2.5f;
        a.HeadModPos = headPos + ((Vector3)headFaceVector * baseModifier * a.Modifier);
        a.TailModPos = tailPos + ((Vector3)tailFaceVector * baseModifier * a.TailModifier * -1); //End point should move in opposite direction of arrow
    }


    public static Vector3 PointOnCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float invT = 1 - t;
        return (Mathf.Pow(invT, 3) * p0) + (3 * Mathf.Pow(invT, 2) * t * p1) + (3 * invT * Mathf.Pow(t, 2) * p2) + (Mathf.Pow(t, 3) * p3);
    }


    public static Vector3[] GetArcPoints(Arc a)
    {
        float startTime = TimeManager.TimeFromBeat(a.Beat);
        float endTime = TimeManager.TimeFromBeat(a.TailBeat);
        float duration = endTime - startTime;

        //Estimate the number of points we'll need to make this arc based on density option
        int pointCount = (int)(ArcSegmentDensity * duration) + 1;
        Vector3[] points = new Vector3[pointCount];
        for(int i = 0; i < pointCount; i++)
        {
            float t = (float)i / (pointCount - 1);
            points[i] = PointOnCubicBezier(a.CurrentHeadPos, a.HeadModPos, a.TailModPos, a.CurrentTailPos, t);
        }

        return points;
    }


    public void UpdateArcVisual(Arc a)
    {
        float arcTime = TimeManager.TimeFromBeat(a.Beat);

        float zDist = objectManager.GetZPosition(arcTime);

        float startHeadY = a.CurrentHeadPos.y;
        float startTailY = a.CurrentTailPos.y;
        if(objectManager.doMovementAnimation)
        {
            //Control points to be updated if this arc is attached to a note
            if(a.HasHeadAttachment)
            {
                float spawnMovement = (a.y - a.StartY) * objectManager.rowHeight;
                float startY = a.HeadPos.y - spawnMovement + objectManager.objectFloorOffset;

                a.CurrentHeadPos.y = objectManager.GetObjectY(startY, a.HeadPos.y, TimeManager.TimeFromBeat(a.Beat));
            }
            if(a.HasTailAttachment)
            {
                float spawnMovement = (a.TailY - a.TailStartY) * objectManager.rowHeight;
                float startY = a.TailPos.y - spawnMovement + objectManager.objectFloorOffset;

                a.CurrentTailPos.y = objectManager.GetObjectY(startY, a.TailPos.y, TimeManager.TimeFromBeat(a.TailBeat));
            }
        }
        else
        {
            //Note movement is off, so arc should be static
            a.CurrentHeadPos = a.HeadPos;
            a.CurrentTailPos = a.TailPos;
        }

        if(a.Visual == null)
        {
            a.Visual = arcPool.GetObject();
            a.Visual.transform.SetParent(arcParent.transform);
            a.Visual.SetActive(true);

            a.arcHandler = a.Visual.GetComponent<ArcHandler>();
            a.arcHandler.SetMaterial(a.Color == 0 ? currentRedArcMaterial : currentBlueArcMaterial, currentArcCenterMaterial);
 
            a.arcHandler.SetArcPoints(GetArcPoints(a));

            RenderedArcs.Add(a);
        }

        if(a.CurrentHeadPos.y != startHeadY || a.CurrentTailPos.y != startTailY)
        {
            //The control points have been changed, update the curve
            a.arcHandler.SetArcPoints(GetArcPoints(a));
        }

        a.Visual.transform.localPosition = new Vector3(0, 0, zDist);
    }


    private void ReleaseArc(Arc a)
    {
        arcPool.ReleaseObject(a.Visual);
        a.Visual = null;
    }


    public void ClearOutsideArcs()
    {
        if(RenderedArcs.Count <= 0)
        {
            return;
        }

        for(int i = RenderedArcs.Count - 1; i >= 0; i--)
        {
            Arc a = RenderedArcs[i];
            if(!objectManager.DurationObjectInSpawnRange(a.Beat, a.TailBeat))
            {
                ReleaseArc(a);
                RenderedArcs.Remove(a);
            }
        }
    }


    public void UpdateArcVisuals(float beat)
    {
        ClearOutsideArcs();

        if(Arcs.Count <= 0)
        {
            return;
        }

        int firstArc = Arcs.FindIndex(x => objectManager.DurationObjectInSpawnRange(x.Beat, x.TailBeat));
        if(firstArc >= 0)
        {
            float lastBeat = 0;
            for(int i = firstArc; i < Arcs.Count; i++)
            {
                Arc a = Arcs[i];
                if(objectManager.DurationObjectInSpawnRange(a.Beat, a.TailBeat))
                {
                    UpdateArcVisual(a);
                    lastBeat = a.TailBeat;
                }
                else if(a.TailBeat - a.Beat <= a.Beat - lastBeat)
                {
                    //Continue looping if this arc overlaps in time with another
                    //This avoids edge cases where two arcs that are close, with one ending before the other causes later arcs to not update
                    //Yes this is the same exact logic as walls
                    break;
                }
            }
        }
    }


    public void ClearRenderedArcs()
    {
        if(RenderedArcs.Count <= 0)
        {
            return;
        }

        foreach(Arc a in RenderedArcs)
        {
            ReleaseArc(a);
        }
        RenderedArcs.Clear();
    }


    private void Start()
    {
        objectManager = ObjectManager.Instance;

        TimeManager.OnBeatChanged += UpdateArcVisuals;
    }
}