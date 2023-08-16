using System.Linq;
using UnityEngine;

public class ArcManager : MapElementManager<Arc>
{
    public static float ArcSegmentDensity => SettingsManager.GetInt("arcdensity");

    [Header("Components")]
    [SerializeField] private ObjectPool<ArcHandler> arcPool;
    [SerializeField] private GameObject arcParent;

    [Header("Parameters")]
    [SerializeField] private Material arcMaterial;

    [SerializeField] private float arcEndFadeStart;
    [SerializeField] private float arcEndFadeEnd;
    [SerializeField] private float arcFadeTransitionLength;
    [SerializeField] private float headlessArcFadeBeats;
    [SerializeField] private float arcAnimationSpeed;

    private Color redArcColor => NoteManager.RedNoteColor;
    private Color blueArcColor => NoteManager.BlueNoteColor;

    private MaterialPropertyBlock redArcMaterialProperties;
    private MaterialPropertyBlock blueArcMaterialProperties;


    public void ReloadArcs()
    {
        ClearRenderedVisuals();
        UpdateMaterials();
    }


    public void UpdateMaterials()
    {
        ClearRenderedVisuals();

        //Sets the distance that arcs should fade out
        const float fadeDistMultiplier = 0.8f;
        float fadeDist = BeatmapManager.HalfJumpDistance * fadeDistMultiplier;

        redArcMaterialProperties.SetFloat("_FadeEndPoint", fadeDist);
        redArcMaterialProperties.SetFloat("_FadeTransitionLength", arcFadeTransitionLength);

        blueArcMaterialProperties.SetFloat("_FadeEndPoint", fadeDist);
        blueArcMaterialProperties.SetFloat("_FadeTransitionLength", arcFadeTransitionLength);

        UpdateVisuals();
    }


    public override void UpdateVisual(Arc a)
    {
        float zDist = objectManager.GetZPosition(a.Time);

        if(a.Visual == null)
        {
            a.arcHandler = arcPool.GetObject();
            a.Visual = a.arcHandler.gameObject;

            a.Visual.transform.SetParent(arcParent.transform);
            a.Visual.SetActive(true);

            a.arcHandler.SetMaterial(arcMaterial, a.Color == 0 ? redArcMaterialProperties : blueArcMaterialProperties);

            a.CalculateBaseCurve();
            a.arcHandler.SetArcPoints(a.BaseCurve);
            a.arcHandler.SetGradient(a.CurveLength, arcEndFadeStart, arcEndFadeEnd);
            a.arcHandler.SetWidth(SettingsManager.GetFloat("arcwidth") / 2);

            RenderedObjects.Add(a);
        }

        bool fadeAnimation = SettingsManager.GetBool("arcfadeanimation");
        bool textureAnimation = SettingsManager.GetBool("arctextureanimation");

        float alpha = SettingsManager.GetFloat("arcbrightness");
        if(fadeAnimation)
        {
            if(!a.HasHeadAttachment)
            {
                float beatDifference = TimeManager.CurrentBeat - a.Beat;
                alpha *= Mathf.Clamp(beatDifference / headlessArcFadeBeats, 0f, 1f);
            }
            else
            {
                const float fullAlphaTime = 0.8f;
                float fullAlphaPos = BeatmapManager.HalfJumpDistance * fullAlphaTime;
                alpha *= 1f - Mathf.Clamp(zDist / fullAlphaPos, 0f, 1f);
            }
        }

        //This arbitrary starting value is just a default for when animations are disabled
        //I picked this because it's a fairly balanced, decent looking variation
        float textureOffset = 0.509f;
        if(textureAnimation)
        {
            float timeDifference = TimeManager.CurrentTime - a.Time;
            float startValue = a.Beat + a.HeadAngle + a.Position.x + a.Position.y;
            textureOffset = (startValue % 1f) + (timeDifference * arcAnimationSpeed);
        }

        Color arcColor = a.Color == 0 ? redArcColor : blueArcColor;
        if(SettingsManager.GetBool("chromaobjectcolors") && a.CustomColor != null)
        {
            //This arc uses a unique chroma color
            arcColor = (Color)a.CustomColor;
        }

        a.arcHandler.SetProperties(alpha, textureOffset, arcColor, objectManager.CutPlanePos);

        if(objectManager.doMovementAnimation)
        {
            //Need to "undo" the player height so startY remains consistent with different heights
            float headStartY = a.HeadStartY - objectManager.playerHeightOffset;
            float tailStartY = a.TailStartY - objectManager.playerHeightOffset;

            float headOffsetY = objectManager.GetObjectY(headStartY, a.Position.y, a.Time) - a.Position.y;
            float tailOffsetY = objectManager.GetObjectY(tailStartY, a.TailPosition.y, a.TailTime) - a.TailPosition.y;

            a.arcHandler.SetArcPoints(GetAdjustedArcCurve(a.BaseCurve, headOffsetY, tailOffsetY, objectManager.NjsMult)); // arc visuals get reset on settings change, so fine to only update in here
        }
        else if(ReplayManager.IsReplayMode)
        {
            //The arc curve still needs to be squashed because of variable NJS
            a.arcHandler.SetArcPoints(GetAdjustedArcCurve(a.BaseCurve, 0f, 0f, objectManager.NjsMult));
        }

        a.Visual.transform.localPosition = new Vector3(0, objectManager.playerHeightOffset, zDist);
    }


    public override bool VisualInSpawnRange(Arc a)
    {
        return objectManager.DurationObjectInSpawnRange(a.Time, a.TailTime, false, false);
    }


    public override void ReleaseVisual(Arc a)
    {
        arcPool.ReleaseObject(a.arcHandler);
        a.arcHandler = null;
        a.Visual = null;
    }


    public override void UpdateVisuals()
    {
        ClearOutsideVisuals();

        if(Objects.Count == 0)
        {
            return;
        }

        int startIndex = GetStartIndex(TimeManager.CurrentTime);
        if(startIndex < 0)
        {
            return;
        }

        float lastBeat = 0;
        for(int i = startIndex; i < Objects.Count; i++)
        {
            Arc a = Objects[i];
            if(objectManager.DurationObjectInSpawnRange(a.Time, a.TailTime, false, false))
            {
                UpdateVisual(a);
                lastBeat = a.TailBeat;
            }
            else if(a.TailBeat - a.Beat <= a.Beat - lastBeat)
            {
                //Continue looping if this arc overlaps in time with another
                //This avoids edge cases where two arcs that overlap,
                //with one starting and ending before the other, causes later arcs to not update
                //Yes this is the same exact logic as walls
                break;
            }
        }
    }


    public static Vector3 PointOnCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float invT = 1 - t;
        return (Mathf.Pow(invT, 3) * p0) + (3 * Mathf.Pow(invT, 2) * t * p1) + (3 * invT * Mathf.Pow(t, 2) * p2) + (Mathf.Pow(t, 3) * p3);
    }


    private static Vector3[] GetArcPointsWithMidRotation(Arc a, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int pointCount)
    {
        const float midPointRotationDeg = 90f;
        const float midPointOffset = 2.5f;
        const float controlXMod = 0.25f;
        const float controlYMod = 0.25f;
        const float controlZMod = 0.15f;

        //Get the midpoint between the two end points; this is the center point which dictates rotation
        Vector3 midPoint = (p0 + p3) / 2;

        //Offset the midpoint based on rotation direction
        bool rotateClockwise = a.MidRotationDirection == ArcRotationDirection.Clockwise;
        float midPointRotation = rotateClockwise ? -midPointRotationDeg : midPointRotationDeg;

        //Directionless arcs shouldn't offset the midpoint at all
        Vector2 headCutDirection = a.HeadDot ? Vector2.zero : ObjectManager.DirectionVector(a.HeadAngle + midPointRotation);
        midPoint += (Vector3)headCutDirection * midPointOffset;

        //Calculate the control points to use for the midPoint
        Vector3 p1Dist = (p1 - midPoint).Abs();
        Vector3 p2Dist = (p2 - midPoint).Abs();

        bool equalXOffset = p1.x.Approximately(p2.x);
        bool equalYOffset = p1.y.Approximately(p2.y);

        //Offset the middle control point based on distances from head and tail control points
        //Don't offset a coordinate if the end control point offsets are equal
        float controlX = equalXOffset ? 0f : (p1Dist.x + p2Dist.x) * controlXMod;
        float controlY = equalYOffset ? 0f : (p1Dist.y + p2Dist.y) * controlYMod;
        float controlZ = (p1Dist.z + p2Dist.z) * controlZMod;

        if(p1.x < p2.x) controlX = -controlX;
        if(p1.y < p2.y) controlY = -controlY;

        Vector3 controlPointOffset = new Vector3(controlX, controlY, -controlZ);
        Vector3 headToMidControl = midPoint + controlPointOffset;
        //The second half of the arc uses a mirrored control point position
        Vector3 midToTailControl = midPoint - controlPointOffset;

        if(pointCount % 2 == 0)
        {
            //A mid anchor requires an odd number of points (even number of segments)
            pointCount++;
        }
        int midPointIndex = (pointCount / 2) + 1;

        Vector3[] points = new Vector3[pointCount];
        for(int i = 0; i < midPointIndex; i++)
        {
            //Calculate the first half of the arc
            float t = (float)i / midPointIndex;
            //Use easing to dedicate more segments to the edges and middle of the arc
            t = Easings.Quad.InOut(t);

            //Use the head control point for the first half
            points[i] = PointOnCubicBezier(p0, p1, headToMidControl, midPoint, t);
        }
        for(int i = midPointIndex; i < pointCount; i++)
        {
            //Calculate the second half of the arc
            float t = (float)(i - midPointIndex) / (pointCount - midPointIndex - 1);
            //Use easing to dedicate more segments to the edges and middle of the arc
            t = Easings.Quad.InOut(t);

            //Use the tail control point for the second half
            points[i] = PointOnCubicBezier(midPoint, midToTailControl, p2, p3, t);
        }

        return points;
    }


    public static Vector3[] GetArcBaseCurve(Arc a)
    {
        float duration = Mathf.Abs(a.TailTime - a.Time);
        float length = ObjectManager.Instance.WorldSpaceFromTime(duration);

        Vector3 p0 = new Vector3(a.Position.x, a.Position.y, 0);
        Vector3 p1 = new Vector3(a.HeadControlPoint.x, a.HeadControlPoint.y, 0);
        Vector3 p2 = new Vector3(a.TailControlPoint.x, a.TailControlPoint.y, length);
        Vector3 p3 = new Vector3(a.TailPosition.x, a.TailPosition.y, length);

        //Calculate the number of points we'll need to make this arc based on the density setting
        //A minimum value is given because very short arcs would otherwise potentially get no segments at all (very bad)
        int pointCount = Mathf.Max((int)ArcSegmentDensity / 2, (int)(ArcSegmentDensity * duration) + 1);
        if(a.MidRotationDirection != ArcRotationDirection.None)
        {
            //Calculating points is different with a midpoint rotation direction
            return GetArcPointsWithMidRotation(a, p0, p1, p2, p3, pointCount);
        }

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


    public static float GetCurveLength(Vector3[] curve)
    {
        if(curve.Length <= 1)
        {
            //When there's only one point the curve has no length
            return 0;
        }

        float length = 0;
        for(int i = 1; i < curve.Length; i++)
        {
            length += (curve[i - 1] - curve[i]).magnitude;
        }
        return length;
    }


    public static Vector3[] GetAdjustedArcCurve(Vector3[] baseCurve, float headOffsetY, float tailOffsetY, float lengthMult)
    {
        if(baseCurve.Length == 0) return baseCurve;

        float arcLength = baseCurve.Last().z;
        float JD = BeatmapManager.HalfJumpDistance;

        //Create a new curve here so we don't overwrite the input
        Vector3[] points = new Vector3[baseCurve.Length];
        for(int i = 0; i < baseCurve.Length; i++)
        {
            Vector3 point = baseCurve[i];

            //Get the preferred offset based on distance from the head
            float headDist = point.z / JD;
            float headT = 1 - Easings.Quad.Out(Mathf.Clamp(headDist, 0, 1));
            float headPreferredOffset = headOffsetY * headT;

            //Get the preferred offset based on distance from the tail
            float tailDist = (arcLength - point.z) / JD;
            float tailT = 1 - Easings.Quad.Out(Mathf.Clamp(tailDist, 0, 1));
            float tailPreferredOffset = tailOffsetY * tailT;

            //Weight the adjustment based on which end of the arc the point is closer to
            float relativePosition = point.z / arcLength;
            point.y += Mathf.Lerp(headPreferredOffset, tailPreferredOffset, relativePosition);

            //Squish the arc if needed
            point.z *= lengthMult;

            points[i] = point;
        }

        return points;
    }


    private void Awake()
    {
        redArcMaterialProperties = new MaterialPropertyBlock();
        blueArcMaterialProperties = new MaterialPropertyBlock();
    }
}


public class Arc : BaseSlider
{
    public Vector3[] BaseCurve { get; private set; }
    public float CurveLength { get; private set; }
    public Vector2 HeadControlPoint;
    public Vector2 TailControlPoint;
    public Vector2 HeadOffsetDirection;
    public Vector2 TailOffsetDirection;
    public float HeadAngle;
    public bool HeadDot;
    public bool HasHeadAttachment;
    public float HeadStartY;
    public float TailStartY;
    public ArcRotationDirection MidRotationDirection;

    public ArcHandler arcHandler;


    public void CalculateBaseCurve()
    {
        BaseCurve = ArcManager.GetArcBaseCurve(this);
        CurveLength = ArcManager.GetCurveLength(BaseCurve);
    }


    public Arc(BeatmapSlider a)
    {
        const float defaultControlOffset = 2.5f;

        Vector2 headPosition = ObjectManager.CalculateObjectPosition(a.x, a.y, a.customData?.coordinates);
        Vector2 tailPosition = ObjectManager.CalculateObjectPosition(a.tx, a.ty, a.customData?.tailCoordinates);

        float headAngle = ObjectManager.CalculateObjectAngle(a.d);
        float tailAngle = ObjectManager.CalculateObjectAngle(a.tc);

        Vector2 headOffsetDirection = a.d == 8 ? Vector2.zero : ObjectManager.DirectionVector(headAngle);
        Vector2 tailOffsetDirection = a.tc == 8 ? Vector2.zero : ObjectManager.DirectionVector(tailAngle) * -1f;

        Vector2 headControlOffset = headOffsetDirection * a.mu * defaultControlOffset;
        Vector2 tailControlOffset = tailOffsetDirection * a.tmu * defaultControlOffset;
        Vector2 headControlPoint = headPosition + headControlOffset;
        Vector2 tailControlPoint = tailPosition + tailControlOffset;

        float headBeat = a.b;
        float tailBeat = a.tb;

        int headCutDirection = a.d;
        int tailCutDirection = a.tc;

        if(tailBeat < headBeat)
        {
            //Negative duration arcs breaks stuff, flip head and tail so they act like regular arcs
            (headBeat, tailBeat) = (tailBeat, headBeat);
            (headPosition, tailPosition) = (tailPosition, headPosition);
            (headAngle, tailAngle) = (tailAngle, headAngle);
            (headCutDirection, tailCutDirection) = (tailCutDirection, headCutDirection);
            (headOffsetDirection, tailOffsetDirection) = (tailOffsetDirection, headOffsetDirection);
            (headControlPoint, tailControlPoint) = (tailControlPoint, headControlPoint);
        }

        ArcRotationDirection rotationDirection = ArcRotationDirection.None;
        if(ObjectManager.SamePlaneAngles(headAngle, tailAngle) && a.x == a.tx)
        {
            //If the angles share the same cut plane, account for rotation direction
            rotationDirection = (ArcRotationDirection)Mathf.Clamp(a.m, 0, 2);
        }

        Beat = headBeat;
        Position = headPosition;
        Color = a.c;
        TailBeat = tailBeat;
        TailPosition = tailPosition;
        HeadControlPoint = headControlPoint;
        TailControlPoint = tailControlPoint;
        HeadOffsetDirection = headOffsetDirection;
        TailOffsetDirection = tailOffsetDirection;
        HeadAngle = headAngle;
        HeadDot = headCutDirection == 8;
        HasHeadAttachment = false;
        HeadStartY = headPosition.y;
        TailStartY = tailPosition.y;
        MidRotationDirection = rotationDirection;

        if(a.customData?.color != null)
        {
            CustomColor = ColorManager.ColorFromCustomDataColor(a.customData.color);
        }
    }
}


public enum ArcRotationDirection
{
    None,
    Clockwise,
    CounterClockwise
}