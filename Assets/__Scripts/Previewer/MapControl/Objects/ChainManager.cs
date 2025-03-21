using System.Collections.Generic;
using UnityEngine;

public class ChainManager : MapElementManager<ChainLink>
{
    [SerializeField] private ObjectPool<ChainLinkHandler> chainLinkPool;

    public MapElementList<Chain> Chains = new MapElementList<Chain>();

    private NoteManager noteManager => objectManager.noteManager;


    public void ReloadChains()
    {
        ClearRenderedVisuals();

        Objects.Clear();
        CustomRTObjects.Clear();
        CustomRTObjects.GetTime = GetSpawnTime;
        foreach(Chain c in Chains)
        {
            CreateChainLinks(c);
        }
        Objects.SortElementsByBeat();
        CustomRTObjects.SortElementsByBeat();

        Objects.ResetStartIndex();
        CustomRTObjects.ResetStartIndex();

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

        MaterialPropertyBlock linkProperties = null;
        MaterialPropertyBlock linkDotProperties = null;
        if(c.CustomColor != null)
        {
            //Premake the link property block to apply it to each link
            linkProperties = new MaterialPropertyBlock();
            linkDotProperties = new MaterialPropertyBlock();
            noteManager.SetNoteMaterialProperties(ref linkProperties, ref linkDotProperties, (Color)c.CustomColor);
        }

        //Keep track of replay events so we don't reuse them
        List<ScoringEvent> usedScoringEvents = new List<ScoringEvent>();

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
                Angle = linkAngle,
                StartY = c.StartY,
                CustomNJS = c.CustomNJS,
                CustomRT = c.CustomRT,
                CustomColor = c.CustomColor,
                CustomNoteProperties = linkProperties,
                CustomDotProperties = linkDotProperties
            };

            if(ReplayManager.IsReplayMode)
            {
                //Links need to be matched up with their corresponding NoteEvents
                List<ScoringEvent> scoringEventsOnBeat = ScoreManager.ScoringEvents.FindAll(x => ObjectManager.CheckSameTime(x.ObjectTime, newLink.Time));

                BeatmapBurstSlider originalSlider = c.burstSlider;

                //The last link gets the actual tail coordinates??????????
                bool isLastElement = i == c.SegmentCount - 1;
                int linkX = isLastElement ? originalSlider.tx : originalSlider.x;
                int linkY = isLastElement ? originalSlider.ty : originalSlider.y;

                int linkID = ((int)ScoringType.ChainLink * 10000) + (linkX * 1000) + (linkY * 100) + (c.Color * 10) + 8;

                ScoringEvent matchingEvent = scoringEventsOnBeat.Find(x => x.ID == linkID && !usedScoringEvents.Contains(x));
                if(matchingEvent == null && isLastElement)
                {
                    //See if this link counts as an arc head
                    linkID -= (int)ScoringType.ChainLink * 10000;
                    linkID += (int)ScoringType.ChainLinkArcHead * 10000;
                    matchingEvent = scoringEventsOnBeat.Find(x => x.ID == linkID && !usedScoringEvents.Contains(x));

                    if(matchingEvent == null)
                    {
                        //Sometimes the last link doesn't get tail coordinates though :smil
                        linkX = originalSlider.x;
                        linkY = originalSlider.y;
                        linkID = ((int)ScoringType.ChainLink * 10000) + (linkX * 1000) + (linkY * 100) + (c.Color * 10) + 8;
                        matchingEvent = scoringEventsOnBeat.Find(x => x.ID == linkID && !usedScoringEvents.Contains(x));

                        if(matchingEvent == null)
                        {
                            //Once again see if this counts as an arc head
                            linkID -= (int)ScoringType.ChainLink * 10000;
                            linkID += (int)ScoringType.ChainLinkArcHead * 10000;
                            matchingEvent = scoringEventsOnBeat.Find(x => x.ID == linkID && !usedScoringEvents.Contains(x));
                        }
                    }
                }

                if(matchingEvent == null)
                {
                    newLink.WasHit = false;
                    newLink.wasMissed = false;
                }
                else
                {
                    if(matchingEvent.noteEventType == NoteEventType.miss)
                    {
                        newLink.WasHit = false;
                        newLink.wasMissed = true;
                    }
                    else
                    {
                        newLink.WasHit = true;
                        newLink.wasMissed = false;
                        newLink.WasBadCut = matchingEvent.noteEventType == NoteEventType.bad;
                        newLink.HitOffset = matchingEvent.HitTimeOffset;
                    }

                    Vector2 worldPosition = objectManager.ObjectSpaceToWorldSpace(newLink.Position);
                    matchingEvent.SetEventValues(ScoringType.ChainLink, worldPosition);

                    usedScoringEvents.Add(matchingEvent);
                }
            }

            if(newLink.CustomRT != null)
            {
                CustomRTObjects.Add(newLink);
            }
            else Objects.Add(newLink);
        }
    }


    public override void UpdateVisual(ChainLink cl)
    {
        float reactionTime = cl.CustomRT ?? jumpManager.ReactionTime;
        float njs = cl.CustomNJS != null
            ? jumpManager.GetAdjustedNJS((float)cl.CustomNJS, reactionTime)
            : jumpManager.EffectiveNJS;
        float halfJumpDistance = jumpManager.WorldSpaceFromTime(reactionTime, njs);

        float worldDist = jumpManager.GetZPosition(cl.Time, njs, reactionTime, halfJumpDistance);
        Vector3 worldPos = new Vector3(cl.Position.x, cl.Position.y, worldDist);

        worldPos.y += objectManager.playerHeightOffset;

        if(objectManager.doMovementAnimation)
        {
            worldPos.y = jumpManager.GetObjectY(cl.StartY, worldPos.y, worldDist, halfJumpDistance, cl.Time, reactionTime);
        }

        float jumpTime = TimeManager.CurrentTime + reactionTime;
        float angle = cl.Angle;

        if(objectManager.doRotationAnimation)
        {
            float rotationAnimationLength = reactionTime * objectManager.rotationAnimationTime;

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
            cl.ChainLinkHandler = chainLinkPool.GetObject();
            cl.Visual = cl.ChainLinkHandler.gameObject;
            cl.source = cl.ChainLinkHandler.audioSource;

            cl.Visual.transform.SetParent(transform);
            cl.ChainLinkHandler.EnableVisual();

            cl.ChainLinkHandler.SetMaterial(objectManager.useSimpleNoteMaterial ? noteManager.simpleMaterial : noteManager.complexMaterial);
            if(SettingsManager.GetBool("chromaobjectcolors") && cl.CustomColor != null)
            {
                //This link uses a unique chroma color
                cl.ChainLinkHandler.SetProperties(cl.CustomNoteProperties);
                cl.ChainLinkHandler.SetDotProperties(cl.CustomDotProperties);
            }
            else
            {
                bool isRed = cl.Color == 0;
                cl.ChainLinkHandler.SetProperties(isRed ? noteManager.redNoteProperties : noteManager.blueNoteProperties);
                cl.ChainLinkHandler.SetDotProperties(isRed ? noteManager.redArrowProperties : noteManager.blueArrowProperties);
            }

            float noteSize = SettingsManager.GetFloat("notesize");
            cl.Visual.transform.localScale = Vector3.one * noteSize;

            cl.Visual.SetActive(true);
            cl.ChainLinkHandler.EnableVisual();

            if(TimeManager.Playing && SettingsManager.GetFloat("hitsoundvolume") > 0 && SettingsManager.GetFloat("chainvolume") > 0)
            {
                HitSoundManager.ScheduleHitsound(cl);
            }

            if(ReplayManager.IsReplayMode && SettingsManager.GetBool("highlighterrors"))
            {
                if(cl.wasMissed)
                {
                    cl.ChainLinkHandler.SetOutline(true, SettingsManager.GetColor("missoutlinecolor"));
                }
                else if(cl.WasBadCut)
                {
                    cl.ChainLinkHandler.SetOutline(true, SettingsManager.GetColor("badcutoutlinecolor"));
                }
                else cl.ChainLinkHandler.SetOutline(false);
            }
            else cl.ChainLinkHandler.SetOutline(false);

            RenderedObjects.Add(cl);
        }

        cl.Visual.transform.localPosition = worldPos;
        cl.Visual.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }


    public override float GetSpawnTime(ChainLink cl)
    {
        return cl.Time - (float)cl.CustomRT - objectManager.moveTime;
    }


    public override bool VisualInSpawnRange(ChainLink cl)
    {
        return jumpManager.CheckInSpawnRange(cl.Time, cl.CustomRT ?? jumpManager.ReactionTime, true, true, cl.HitOffset);
    }


    public override void ReleaseVisual(ChainLink cl)
    {
        cl.source.Stop();
        chainLinkPool.ReleaseObject(cl.ChainLinkHandler);

        cl.Visual = null;
        cl.source = null;
        cl.ChainLinkHandler = null;
    }


    public override void ClearOutsideVisuals()
    {
        for(int i = RenderedObjects.Count - 1; i >= 0; i--)
        {
            ChainLink cl = RenderedObjects[i];
            if(!jumpManager.CheckInSpawnRange(cl.Time, cl.CustomRT ?? jumpManager.ReactionTime, !cl.WasHit, true, cl.HitOffset))
            {
                if(cl.source.isPlaying || (ReplayManager.IsReplayMode && cl.Time > TimeManager.CurrentTime && cl.Time < TimeManager.CurrentTime + 0.5f))
                {
                    //Only clear the visual elements if the hitsound is still playing
                    cl.ChainLinkHandler.DisableVisual();
                }
                else
                {
                    ReleaseVisual(cl);
                    RenderedObjects.Remove(cl);
                }
            }
            else cl.ChainLinkHandler.EnableVisual();
        }
    }


    public override void UpdateObjects(MapElementList<ChainLink> objects)
    {
        if(objects.Count == 0)
        {
            return;
        }

        int startIndex = GetStartIndex(TimeManager.CurrentTime, objects);
        if(startIndex < 0)
        {
            return;
        }

        for(int i = startIndex; i < objects.Count; i++)
        {
            //Update each link's position
            ChainLink cl = objects[i];
            if(jumpManager.CheckInSpawnRange(cl.Time, cl.CustomRT ?? jumpManager.ReactionTime, !cl.WasHit, true, cl.HitOffset))
            {
                UpdateVisual(cl);
            }
            else if(!VisualInSpawnRange(cl))
            {
                break;
            }
        }
    }


    public void RescheduleHitsounds()
    {
        foreach(ChainLink cl in RenderedObjects)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if(cl.source != null)
#endif
            {
                HitSoundManager.ScheduleHitsound(cl);
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
    public float StartY;

    //I really don't wanna keep this around but it's necessary for replays
    public BeatmapBurstSlider burstSlider;


    public Chain(BeatmapBurstSlider b)
    {
        Vector2 headPosition = ObjectManager.CalculateObjectPosition(b.x, b.y, b.customData?.coordinates, false);
        Vector2 tailPosition = ObjectManager.CalculateObjectPosition(b.tx, b.ty, b.customData?.tailCoordinates, false);
        float angle = ObjectManager.CalculateObjectAngle(b.d);

        Beat = b.b;
        Position = headPosition;
        Color = b.c;
        Angle = angle;
        StartY = ObjectManager.Instance.objectFloorOffset;
        TailBeat = b.tb;
        TailPosition = tailPosition;
        SegmentCount = b.sc;
        Squish = b.s < 0.001f ? 1f : b.s;

        burstSlider = b;

        if(b.customData != null)
        {
            if(b.customData.color != null)
            {
                CustomColor = ColorManager.ColorFromCustomDataColor(b.customData.color);
            }

            CustomNJS = b.customData.noteJumpMovementSpeed;
            if(b.customData.noteJumpStartBeatOffset != null)
            {
                CustomRT = BeatmapManager.GetCustomRT((float)b.customData.noteJumpStartBeatOffset);
            }
        }
    }
}


public class ChainLink : HitSoundEmitter
{
    public int Color;
    public float Angle;
    public float StartY;

    public ChainLinkHandler ChainLinkHandler;
    public MaterialPropertyBlock CustomNoteProperties;
    public MaterialPropertyBlock CustomDotProperties;


    public ChainLink()
    {
        Color = 0;
        Angle = 0f;

        WasHit = true;
        WasBadCut = false;
        HitOffset = 0f;
    }
}