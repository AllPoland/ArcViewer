using System.Collections.Generic;
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


    public void ReloadArcs()
    {
        ClearRenderedArcs();
        arcPool.SetPoolSize(20);

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


    public static Vector3 PointOnCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float invT = 1 - t;
        return (Mathf.Pow(invT, 3) * p0) + (3 * Mathf.Pow(invT, 2) * t * p1) + (3 * invT * Mathf.Pow(t, 2) * p2) + (Mathf.Pow(t, 3) * p3);
    }


    public static Vector3[] GetArcPoints(Arc a, float headOffsetY = 0, float tailOffsetY = 0)
    {
        float startTime = TimeManager.TimeFromBeat(a.Beat);
        float endTime = TimeManager.TimeFromBeat(a.TailBeat);
        float duration = endTime - startTime;
        float length = objectManager.WorldSpaceFromTime(duration);

        Vector3 p0 = new Vector3(a.Position.x, a.Position.y + headOffsetY, 0);
        Vector3 p1 = new Vector3(a.HeadControlPoint.x, a.HeadControlPoint.y + headOffsetY, 0);
        Vector3 p2 = new Vector3(a.TailControlPoint.x, a.TailControlPoint.y + tailOffsetY, length);
        Vector3 p3 = new Vector3(a.TailPosition.x, a.TailPosition.y + tailOffsetY, length);

        //Estimate the number of points we'll need to make this arc based on density option
        //A minimum value is given because very short arcs would otherwise potentially get no segments at all (very bad)
        int pointCount = Mathf.Max(10, (int)(ArcSegmentDensity * duration) + 1);
        Vector3[] points = new Vector3[pointCount];
        for(int i = 0; i < pointCount; i++)
        {
            float t = (float)i / (pointCount - 1);

            //Easing here dedicates more segments to the edges of the arc, where more curvature is present
            t = Easings.Quad.InOut(t);

            points[i] = PointOnCubicBezier(p0, p1, p2, p3, t);
        }

        return points;
    }


    public void UpdateArcVisual(Arc a)
    {
        float arcTime = TimeManager.TimeFromBeat(a.Beat);

        float zDist = objectManager.GetZPosition(arcTime);

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

        if(objectManager.doMovementAnimation)
        {
            float headOffsetY = objectManager.GetObjectY(a.HeadStartY, a.Position.y, TimeManager.TimeFromBeat(a.Beat)) - a.Position.y;

            float tailOffsetY = objectManager.GetObjectY(a.TailStartY, a.TailPosition.y, TimeManager.TimeFromBeat(a.TailBeat)) - a.TailPosition.y;

            a.arcHandler.SetArcPoints(GetArcPoints(a, headOffsetY, tailOffsetY)); // arc visuals get reset on settings change, so fine to only update in here
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


public class Arc : BaseSlider
{
    public Vector2 HeadControlPoint;
    public Vector2 TailControlPoint;
    public float HeadStartY;
    public float TailStartY;
    public int MiddleRotationDirection; // currently unimplemented

    public ArcHandler arcHandler;


    public static Arc ArcFromBeatmapSlider(BeatmapSlider a)
    {
        Vector2 headPosition = ObjectManager.CalculateObjectPosition(a.x, a.y);
        Vector2 tailPosition = ObjectManager.CalculateObjectPosition(a.tx, a.ty);
        Vector2 headControlPoint = headPosition + ObjectManager.DirectionVector(ObjectManager.CalculateObjectAngle(a.d)) * a.mu * 2.5f;
        Vector2 tailControlPoint = tailPosition - ObjectManager.DirectionVector(ObjectManager.CalculateObjectAngle(a.tc)) * a.tmu * 2.5f;

        float headBeat = a.b;
        float tailBeat = a.tb;

        if(tailBeat < headBeat)
        {
            //Negative duration arcs breaks stuff, flip head and tail so they act like regular arcs
            (headBeat, tailBeat) = (tailBeat, headBeat);
        }

        return new Arc
        {
            Beat = headBeat,
            Position = headPosition,
            Color = a.c,
            TailBeat = tailBeat,
            TailPosition = tailPosition,
            HeadControlPoint = a.d == 8 ? headPosition : headControlPoint,
            TailControlPoint = a.tc == 8 ? tailPosition : tailControlPoint,
            HeadStartY = headPosition.y,
            TailStartY = tailPosition.y,
            MiddleRotationDirection = a.m
        };
    }
}