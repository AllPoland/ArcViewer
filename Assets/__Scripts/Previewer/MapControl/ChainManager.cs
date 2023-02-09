using System.Collections.Generic;
using UnityEngine;

public class ChainManager : MonoBehaviour
{
    [Header("Pools")]
    [SerializeField] private ObjectPool chainLinkPool;

    [Header("Object Parents")]
    [SerializeField] private GameObject linkParent;

    [Header("Materials")]
    [SerializeField] private Material complexMaterialRed;
    [SerializeField] private Material complexMaterialBlue;
    [SerializeField] private Material simpleMaterialRed;
    [SerializeField] private Material simpleMaterialBlue;
    [SerializeField] private Material arrowMaterialRed;
    [SerializeField] private Material arrowMaterialBlue;

    public List<Chain> Chains = new List<Chain>();
    public List<ChainLink> ChainLinks = new List<ChainLink>();
    public List<ChainLink> RenderedChainLinks = new List<ChainLink>();

    

    private ObjectManager objectManager;


    public void ReloadChains()
    {
        ClearRenderedLinks();
        chainLinkPool.SetPoolSize(60);

        ChainLinks.Clear();

        foreach(Chain c in Chains)
        {
            CreateChainLinks(c);
        }
        ChainLinks = ObjectManager.SortObjectsByBeat(ChainLinks);

        UpdateChainVisuals(TimeManager.CurrentBeat);
    }



    public static Vector2 QuadBezierPoint(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        return (Mathf.Pow(1 - t, 2) * p0) + (2 * (1 - t) * t * p1) + (Mathf.Pow(t, 2) * p2);
    }


    public static float AngleOnQuadBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        Vector2 derivative = (2 * (1 - t) * (p1 - p0)) + (2 * t * (p2 - p1));
        return Mathf.Rad2Deg * Mathf.Atan2(derivative.x, -derivative.y);
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
            Vector2 linkPos = QuadBezierPoint(startPos, midPoint, endPos, t);
            float linkAngle = AngleOnQuadBezier(startPos, midPoint, endPos, t);

            ChainLink newLink = new ChainLink
            {
                Beat = beat,
                Position = linkPos,
                Color = c.Color,
                Angle = linkAngle
            };
            ChainLinks.Add(newLink);
        }
    }


    public void UpdateLinkVisual(ChainLink cl)
    {
        //Calculate the Z position based on time
        float linkTime = TimeManager.TimeFromBeat(cl.Beat);

        float reactionTime = BeatmapManager.ReactionTime;
        float jumpTime = TimeManager.CurrentTime + reactionTime;

        float worldDist = objectManager.GetZPosition(linkTime);
        Vector3 worldPos = new Vector3(cl.Position.x, cl.Position.y, worldDist);

        if(objectManager.doMovementAnimation)
        {
            float startY = objectManager.objectFloorOffset;
            worldPos.y = objectManager.GetObjectY(startY, worldPos.y, linkTime);
        }

        float angle = cl.Angle;
        float rotationAnimationLength = reactionTime * objectManager.rotationAnimationTime;

        if(objectManager.doRotationAnimation)
        {
            if(linkTime > jumpTime)
            {
                //Note is still jumping in
                angle = 0;
            }
            else if(linkTime > jumpTime - rotationAnimationLength)
            {
                float timeSinceJump = reactionTime - (linkTime - TimeManager.CurrentTime);
                float rotationProgress = timeSinceJump / rotationAnimationLength;
                float angleDist = Easings.Sine.Out(rotationProgress);

                angle *= angleDist;
            }
        }

        if(cl.Visual == null)
        {
            cl.Visual = chainLinkPool.GetObject();
            cl.Visual.transform.SetParent(linkParent.transform);

            cl.chainLinkHandler = cl.Visual.GetComponent<ChainLinkHandler>();
            cl.source = cl.chainLinkHandler.audioSource;

            cl.chainLinkHandler.SetDotMaterial(cl.Color == 0 ? arrowMaterialRed : arrowMaterialBlue);

            if(objectManager.useSimpleNoteMaterial)
            {
                cl.chainLinkHandler.SetMaterial(cl.Color == 0 ? simpleMaterialRed : simpleMaterialBlue);
            }
            else
            {
                cl.chainLinkHandler.SetMaterial(cl.Color == 0 ? complexMaterialRed : complexMaterialBlue);
            }

            cl.Visual.SetActive(true);
            cl.chainLinkHandler.EnableVisual();

            if(TimeManager.Playing && SettingsManager.GetFloat("hitsoundvolume") > 0 && SettingsManager.GetFloat("chainvolume") > 0)
            {
                HitSoundManager.ScheduleHitsound(linkTime, cl.source);
            }

            RenderedChainLinks.Add(cl);
        }

        cl.Visual.transform.localPosition = worldPos;
        cl.Visual.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }


    private void ReleaseChainLink(ChainLink cl)
    {
        cl.source.Stop();
        chainLinkPool.ReleaseObject(cl.Visual);

        cl.Visual = null;
        cl.source = null;
        cl.chainLinkHandler = null;
    }


    public void ClearRenderedLinks()
    {
        if(RenderedChainLinks.Count <= 0)
        {
            return;
        }

        foreach(ChainLink cl in RenderedChainLinks)
        {
            ReleaseChainLink(cl);
        }
        RenderedChainLinks.Clear();
    }


    public void ClearOutsideLinks()
    {
        if(RenderedChainLinks.Count <= 0)
        {
            return;
        }

        for(int i = RenderedChainLinks.Count - 1; i >= 0; i--)
        {
            ChainLink cl = RenderedChainLinks[i];
            if(!objectManager.CheckInSpawnRange(cl.Beat))
            {
                if(cl.source.isPlaying)
                {
                    //Only clear the visual elements if the hitsound is still playing
                    cl.chainLinkHandler.DisableVisual();
                    continue;
                }

                ReleaseChainLink(cl);
                RenderedChainLinks.Remove(cl);
            }
            else if(!cl.chainLinkHandler.Visible)
            {
                cl.chainLinkHandler.EnableVisual();
            }
        }
    }


    public void UpdateChainVisuals(float beat)
    {
        ClearOutsideLinks();

        if(ChainLinks.Count <= 0)
        {
            return;
        }

        int firstLink = ChainLinks.FindIndex(x => objectManager.CheckInSpawnRange(x.Beat));
        if(firstLink >= 0)
        {
            for(int i = firstLink; i < ChainLinks.Count; i++)
            {
                //Update each link's position
                ChainLink cl = ChainLinks[i];
                if(objectManager.CheckInSpawnRange(cl.Beat))
                {
                    UpdateLinkVisual(cl);
                }
                else break;
            }
        }
    }


    public void RescheduleHitsounds(bool playing)
    {
        if(!playing)
        {
            return;
        }

        foreach(ChainLink cl in RenderedChainLinks)
        {
            if(cl.source != null && SettingsManager.GetFloat("hitsoundvolume") > 0 && SettingsManager.GetFloat("chainvolume") > 0)
            {
                HitSoundManager.ScheduleHitsound(TimeManager.TimeFromBeat(cl.Beat), cl.source);
            }
        }
    }


    private void Start()
    {
        objectManager = ObjectManager.Instance;

        TimeManager.OnBeatChanged += UpdateChainVisuals;
        TimeManager.OnPlayingChanged += RescheduleHitsounds;
    }
}


public class Chain : BaseSlider
{
    public float Angle;
    public int SegmentCount;
    public float Squish;


    public static Chain ChainFromBeatmapBurstSlider(BeatmapBurstSlider b)
    {
        Vector2 headPosition = ObjectManager.CalculateObjectPosition(b.x, b.y);
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