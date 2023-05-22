using UnityEngine;

public class ChainManager : MapElementManager<ChainLink>
{
    [SerializeField] private ObjectPool<ChainLinkHandler> chainLinkPool;

    public MapElementList<Chain> Chains = new MapElementList<Chain>();


    public void ReloadChains()
    {
        ClearRenderedVisuals();
        chainLinkPool.SetPoolSize(60);

        Objects.Clear();

        foreach(Chain c in Chains)
        {
            CreateChainLinks(c);
        }
        Objects.SortElementsByBeat();

        UpdateVisuals();
    }


    private void CreateChainLinks(Chain c)
    {
        //These are the start and end points of the bezier curve
        Vector2 startPos = c.Position;
        Vector2 endPos = c.TailPosition;

        //The midpoint of the curve is 1/2 the distance between the start points, in the direction the chain faces
        float directDistance = Vector2.Distance(startPos, endPos);

        Vector2 midOffset = ObjectManager.DirectionVector(c.Angle) * directDistance / 2;
        Vector2 midPoint = startPos + midOffset;

        float duration = c.TailBeat - c.Beat;

        //Start at 1 because head note counts as a "segment"
        for(int i = 1; i < c.SegmentCount; i++)
        {
            float timeProgress = (float)i / (c.SegmentCount - 1);

            //Calculate beat based on time progress
            float beat = c.Beat + (duration * timeProgress);

            //Calculate position based on the chain's bezier curve
            float t = timeProgress * c.Squish;
            Vector2 linkPos = PointOnQuadBezier(startPos, midPoint, endPos, t);
            float linkAngle = AngleOnQuadBezier(startPos, midPoint, endPos, t);

            ChainLink newLink = new ChainLink
            {
                Beat = beat,
                Position = linkPos,
                Color = c.Color,
                Angle = linkAngle
            };
            Objects.Add(newLink);
        }
    }


    public override void UpdateVisual(ChainLink cl)
    {
        float reactionTime = BeatmapManager.ReactionTime;
        float jumpTime = TimeManager.CurrentTime + reactionTime;

        float worldDist = objectManager.GetZPosition(cl.Time);
        Vector3 worldPos = new Vector3(cl.Position.x, cl.Position.y, worldDist);

        if(objectManager.doMovementAnimation)
        {
            float startY = objectManager.objectFloorOffset;
            worldPos.y = objectManager.GetObjectY(startY, worldPos.y, cl.Time);
        }

        float angle = cl.Angle;
        float rotationAnimationLength = reactionTime * objectManager.rotationAnimationTime;

        if(objectManager.doRotationAnimation)
        {
            if(cl.Time > jumpTime)
            {
                //Note is still jumping in
                angle = 0;
            }
            else if(cl.Time > jumpTime - rotationAnimationLength)
            {
                float timeSinceJump = reactionTime - (cl.Time - TimeManager.CurrentTime);
                float rotationProgress = timeSinceJump / rotationAnimationLength;
                float angleDist = Easings.Sine.Out(rotationProgress);

                angle *= angleDist;
            }
        }

        if(cl.Visual == null)
        {
            cl.chainLinkHandler = chainLinkPool.GetObject();
            cl.Visual = cl.chainLinkHandler.gameObject;

            cl.Visual.transform.SetParent(transform);
            cl.source = cl.chainLinkHandler.audioSource;

            NoteManager noteManager = objectManager.noteManager;
            bool isRed = cl.Color == 0;
            cl.chainLinkHandler.SetMaterial(objectManager.useSimpleNoteMaterial ? noteManager.simpleMaterial : noteManager.complexMaterial);
            cl.chainLinkHandler.SetProperties(isRed ? noteManager.redNoteProperties : noteManager.blueNoteProperties);
            cl.chainLinkHandler.SetDotProperties(isRed ? noteManager.redArrowProperties : noteManager.blueArrowProperties);

            cl.Visual.SetActive(true);
            cl.chainLinkHandler.EnableVisual();

            if(TimeManager.Playing && SettingsManager.GetFloat("hitsoundvolume") > 0 && SettingsManager.GetFloat("chainvolume") > 0)
            {
                HitSoundManager.ScheduleHitsound(cl.Time, cl.source);
            }

            RenderedObjects.Add(cl);
        }

        cl.Visual.transform.localPosition = worldPos;
        cl.Visual.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }


    public override bool VisualInSpawnRange(ChainLink cl)
    {
        return objectManager.CheckInSpawnRange(cl.Time);
    }


    public override void ReleaseVisual(ChainLink cl)
    {
        cl.source.Stop();
        chainLinkPool.ReleaseObject(cl.chainLinkHandler);

        cl.Visual = null;
        cl.source = null;
        cl.chainLinkHandler = null;
    }


    public override void ClearOutsideVisuals()
    {
        for(int i = RenderedObjects.Count - 1; i >= 0; i--)
        {
            ChainLink cl = RenderedObjects[i];
            if(!objectManager.CheckInSpawnRange(cl.Time))
            {
                if(cl.source.isPlaying)
                {
                    //Only clear the visual elements if the hitsound is still playing
                    cl.chainLinkHandler.DisableVisual();
                    continue;
                }

                ReleaseVisual(cl);
                RenderedObjects.Remove(cl);
            }
            else if(!cl.chainLinkHandler.Visible)
            {
                cl.chainLinkHandler.EnableVisual();
            }
        }
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

        for(int i = startIndex; i < Objects.Count; i++)
        {
            //Update each link's position
            ChainLink cl = Objects[i];
            if(objectManager.CheckInSpawnRange(cl.Time))
            {
                UpdateVisual(cl);
            }
            else break;
        }
    }


    public void RescheduleHitsounds()
    {
        foreach(ChainLink cl in RenderedObjects)
        {
            if(cl.source != null && SettingsManager.GetFloat("hitsoundvolume") > 0 && SettingsManager.GetFloat("chainvolume") > 0)
            {
                HitSoundManager.ScheduleHitsound(cl.Time, cl.source);
            }
        }
    }


    public static Vector2 PointOnQuadBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        return (Mathf.Pow(1 - t, 2) * p0) + (2 * (1 - t) * t * p1) + (Mathf.Pow(t, 2) * p2);
    }


    public static float AngleOnQuadBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        Vector2 derivative = (2 * (1 - t) * (p1 - p0)) + (2 * t * (p2 - p1));
        return Mathf.Rad2Deg * Mathf.Atan2(derivative.x, -derivative.y);
    }
}


public class Chain : BaseSlider
{
    public float Angle;
    public int SegmentCount;
    public float Squish;


    public static Chain ChainFromBeatmapBurstSlider(BeatmapBurstSlider b)
    {
        Vector2 headPosition = ObjectManager.CalculateObjectPosition(b.x, b.y, b.customData?.coordinates);
        Vector2 tailPosition = ObjectManager.CalculateObjectPosition(b.tx, b.ty);
        float angle = ObjectManager.CalculateObjectAngle(b.d);

        return new Chain
        {
            Beat = b.b,
            Position = headPosition,
            Color = b.c,
            Angle = angle,
            TailBeat = b.tb,
            TailPosition = tailPosition,
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