using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NoteManager : MapElementManager<Note>
{
    public static Color RedNoteColor => ColorManager.CurrentColors.LeftNoteColor;
    public static Color BlueNoteColor => ColorManager.CurrentColors.RightNoteColor;

    public static Color LeftSaberColor;
    public static Color RightSaberColor;
    public static event Action OnSaberColorsChanged;

    [SerializeField] private ObjectPool<NoteHandler> notePool;

    [Header("Meshes")]
    [SerializeField] private Mesh noteMesh;
    [SerializeField] private Mesh chainHeadMesh;

    [Header("Materials")]
    [SerializeField] public Material complexMaterial;
    [SerializeField] public Material simpleMaterial;
    [SerializeField] private float noteEmission;
    [SerializeField] private float simpleNoteSaturation;
    [SerializeField] private float simpleNoteEmission;
    [SerializeField, Range(0f, 1f)] private float arrowSaturation;
    [SerializeField, Range(0f, 1f)] private float arrowBrightness;
    [SerializeField, Range(0f, 1f)] private float arrowGlowSaturation;
    [SerializeField] private float arrowEmission;

    //These are all public so ChainManager can access them
    public MaterialPropertyBlock redNoteProperties;
    public MaterialPropertyBlock blueNoteProperties;
    public MaterialPropertyBlock redArrowProperties;
    public MaterialPropertyBlock blueArrowProperties;

    private Note firstLeftNote;
    private Note firstRightNote;


    public void ReloadNotes()
    {
        CustomRTObjects.Clear();
        CustomRTObjects.GetTime = GetSpawnTime;
        for(int i = Objects.Count - 1; i >= 0; i--)
        {
            Note n = Objects[i];
            if(n.CustomRT != null)
            {
                Objects.Remove(n);
                CustomRTObjects.Add(n);
            }
        }
        CustomRTObjects.SortElementsByBeat();

        Objects.ResetStartIndex();
        CustomRTObjects.ResetStartIndex();

        ClearRenderedVisuals();
        UpdateMaterials();
    }


    public void UpdateMaterials()
    {
        ClearRenderedVisuals();

        SetNoteMaterialProperties(ref redNoteProperties, ref redArrowProperties, RedNoteColor);
        SetNoteMaterialProperties(ref blueNoteProperties, ref blueArrowProperties, BlueNoteColor);

        UpdateVisuals();
    }


    public void SetNoteMaterialProperties(ref MaterialPropertyBlock noteProperties, ref MaterialPropertyBlock arrowProperties, Color baseColor)
    {
        float h, s, v;
        Color.RGBToHSV(baseColor, out h, out s, out v);

        float saturation = objectManager.useSimpleNoteMaterial ? simpleNoteSaturation : 1f;
        float emission = objectManager.useSimpleNoteMaterial ? simpleNoteEmission : noteEmission;
        noteProperties.SetColor("_BaseColor", baseColor.SetSaturation(saturation * s));
        noteProperties.SetColor("_EmissionColor", baseColor.SetHSV(null, saturation * s, emission * v, true));

        arrowProperties.SetColor("_BaseColor", baseColor.SetHSV(null, arrowSaturation * s, arrowBrightness * v));
        arrowProperties.SetColor("_EmissionColor", baseColor.SetHSV(null, arrowGlowSaturation * s, arrowEmission, true));
    }


    public override void UpdateVisual(Note n)
    {
        float reactionTime = n.CustomRT ?? jumpManager.ReactionTime;
        float njs = n.CustomNJS != null
            ? jumpManager.GetAdjustedNJS((float)n.CustomNJS, reactionTime)
            : jumpManager.EffectiveNJS;
        float halfJumpDistance = jumpManager.WorldSpaceFromTime(reactionTime, njs);

        float worldDist = jumpManager.GetZPosition(n.Time, njs, reactionTime, halfJumpDistance);
        Vector3 worldPos = new Vector3(n.Position.x, n.Position.y, worldDist);

        worldPos.y += objectManager.playerHeightOffset;

        if(objectManager.doMovementAnimation)
        {
            worldPos.y = jumpManager.GetObjectY(n.StartY, worldPos.y, worldDist, halfJumpDistance, n.Time, reactionTime);
        }

        float angle = n.Angle;

        float jumpTime = TimeManager.CurrentTime + reactionTime;
        float jumpProgress = (jumpTime - n.Time) / reactionTime;

        if(objectManager.doFlipAnimation && n.FlipYHeight != 0)
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
        Quaternion worldRotation = Quaternion.AngleAxis(angle, Vector3.forward);

        if(n.Visual == null)
        {
            n.NoteHandler = notePool.GetObject();
            n.Visual = n.NoteHandler.gameObject;
            n.source = n.NoteHandler.audioSource;

            n.Visual.transform.SetParent(transform);
            n.NoteHandler.EnableVisual();

            n.NoteHandler.SetMesh(n.IsChainHead ? chainHeadMesh : noteMesh);
            n.NoteHandler.SetArrow(!n.IsDot);

            n.NoteHandler.SetMaterial(objectManager.useSimpleNoteMaterial ? simpleMaterial : complexMaterial);
            if(SettingsManager.GetBool("chromaobjectcolors") && n.CustomColor != null)
            {
                //This note uses a unique chroma color
                n.NoteHandler.SetProperties(n.CustomNoteProperties);
                n.NoteHandler.SetArrowProperties(n.CustomArrowProperties);
            }
            else
            {
                bool isRed = n.Color == 0;
                n.NoteHandler.SetProperties(isRed ? redNoteProperties : blueNoteProperties);
                n.NoteHandler.SetArrowProperties(isRed ? redArrowProperties : blueArrowProperties);
            }

            float noteSize = SettingsManager.GetFloat("notesize");
            n.Visual.transform.localScale = Vector3.one * noteSize;

            n.Visual.SetActive(true);
            n.NoteHandler.EnableVisual();

            if(TimeManager.Playing && SettingsManager.GetFloat("hitsoundvolume") > 0)
            {
                HitSoundManager.ScheduleHitsound(n);
            }

            if(ReplayManager.IsReplayMode && SettingsManager.GetBool("highlighterrors"))
            {
                if(n.wasMissed)
                {
                    n.NoteHandler.SetOutline(true, SettingsManager.GetColor("missoutlinecolor"));
                }
                else if(n.WasBadCut)
                {
                    n.NoteHandler.SetOutline(true, SettingsManager.GetColor("badcutoutlinecolor"));
                }
                else n.NoteHandler.SetOutline(false);
            }
            else n.NoteHandler.SetOutline(false);

            RenderedObjects.Add(n);
        }

        n.Visual.transform.localPosition = worldPos;

        if(objectManager.doLookAnimation && !n.IsChainHead)
        {
            //Notes look towards the player's head in replays
            if(jumpProgress < 1f)
            {
                n.Visual.transform.localRotation = objectManager.LookAtPlayer(n.Visual.transform.position, PlayerPositionManager.HeadPosition, worldRotation, jumpProgress);
            }
            else
            {
                //Notes stop rotating after passing the player's head
                //Recreate the positions when the note passed the cut plane
                Vector3 endRotationPosition = objectManager.ObjectSpaceToWorldSpace(n.Position);
                endRotationPosition.z = n.EndHeadPosition.z + ObjectManager.PlayerCutPlaneDistance;

                n.Visual.transform.localRotation = objectManager.LookAtPlayer(endRotationPosition, n.EndHeadPosition, worldRotation, jumpProgress);
            }
        }
        else
        {
            n.Visual.transform.localRotation = worldRotation;
        }
    }


    public override float GetSpawnTime(Note n)
    {
        return n.Time - (float)n.CustomRT - objectManager.moveTime;
    }


    public override bool VisualInSpawnRange(Note n)
    {
        return jumpManager.CheckInSpawnRange(n.Time, n.CustomRT ?? jumpManager.ReactionTime, true, true, n.HitOffset);
    }


    public override void ReleaseVisual(Note n)
    {
        n.source.Stop();
        notePool.ReleaseObject(n.NoteHandler);

        n.Visual = null;
        n.source = null;
        n.NoteHandler = null;
    }


    public override void ClearOutsideVisuals()
    {
        for(int i = RenderedObjects.Count - 1; i >= 0; i--)
        {
            Note n = RenderedObjects[i];
            if(!jumpManager.CheckInSpawnRange(n.Time, n.CustomRT ?? jumpManager.ReactionTime, !n.WasHit, true, n.HitOffset))
            {
                if(n.source.isPlaying || (ReplayManager.IsReplayMode && n.Time > TimeManager.CurrentTime && n.Time < TimeManager.CurrentTime + 0.5f))
                {
                    //Only clear the visual elements if the hitsound is still playing
                    n.NoteHandler.DisableVisual();
                }
                else
                {
                    ReleaseVisual(n);
                    RenderedObjects.Remove(n);
                }
            }
            else n.NoteHandler.EnableVisual();
        }
    }


    private void UpdateSaberColors()
    {
        Color leftColor = RedNoteColor;
        Color rightColor = BlueNoteColor;
        if(SettingsManager.GetBool("chromaobjectcolors"))
        {
            leftColor = firstLeftNote?.CustomNoteProperties?.GetColor("_BaseColor") ?? RedNoteColor;
            rightColor = firstRightNote?.CustomNoteProperties?.GetColor("_BaseColor") ?? BlueNoteColor;
        }

        if(leftColor != LeftSaberColor || rightColor != RightSaberColor)
        {
            LeftSaberColor = leftColor;
            RightSaberColor = rightColor;

            OnSaberColorsChanged?.Invoke();
        }
    }


    public override void UpdateVisuals()
    {
        ClearOutsideVisuals();

        firstLeftNote = null;
        firstRightNote = null;
        if(Objects.Count == 0 && CustomRTObjects.Count == 0)
        {
            UpdateSaberColors();
            return;
        }

        UpdateObjects(Objects);
        UpdateObjects(CustomRTObjects);

        UpdateSaberColors();
    }


    public override void UpdateObjects(MapElementList<Note> objects)
    {
        int startIndex = GetStartIndex(TimeManager.CurrentTime, objects);
        if(startIndex < 0)
        {
            return;
        }

        bool useChroma = SettingsManager.GetBool("chromaobjectcolors");
        for(int i = startIndex; i < objects.Count; i++)
        {
            //Update each note's position
            Note n = objects[i];
            if(jumpManager.CheckInSpawnRange(n.Time, n.CustomRT ?? jumpManager.ReactionTime, !n.WasHit, true, n.HitOffset))
            {
                UpdateVisual(n);

                if(useChroma)
                {
                    if(n.Color == 0 && (firstLeftNote == null || firstLeftNote.Time > n.Time))
                    {
                        firstLeftNote = n;
                    }
                    else if(n.Color == 1 && (firstRightNote == null || firstRightNote.Time > n.Time))
                    {
                        firstRightNote = n;
                    }
                }
            }
            else if(!VisualInSpawnRange(n))
            {
                break;
            }
        }

        if(useChroma && (firstLeftNote == null || firstRightNote == null))
        {
            //If the next notes haven't spawned yet, keep the color of the previous note
            for(int i = startIndex; i >= 0; i--)
            {
                Note n = objects[i];
                if(n.Color == 0 && firstLeftNote == null)
                {
                    firstLeftNote = n;
                }
                else if(n.Color == 1 && firstRightNote == null)
                {
                    firstRightNote = n;
                }

                if(firstLeftNote != null && firstRightNote != null)
                {
                    break;
                }
            }
        }
    }


    public void RescheduleHitsounds()
    {
        foreach(Note n in RenderedObjects)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if(n.source != null)
#endif
            {
                HitSoundManager.ScheduleHitsound(n);
            }
        }
    }


    public static int GetStartY(BeatmapObject n, List<BeatmapObject> sameBeatObjects)
    {
        List<BeatmapObject> objectsOnBeat = new List<BeatmapObject>(sameBeatObjects);

        if(n.y <= 0) return 0;

        //Remove all notes that aren't directly below this one
        objectsOnBeat.RemoveAll(x => x.x != n.x || x.y >= n.y);

        if(objectsOnBeat.Count == 0) return 0;

        //Need to recursively calculate the startYs of each note underneath
        return objectsOnBeat.Max(x => GetStartY(x, objectsOnBeat)) + 1;
    }


    public static Chain CheckChainHead(BeatmapColorNote n, List<BeatmapBurstSlider> sameBeatBurstSliders, List<Chain> sameBeatChains)
    {
        float[] coordinates = n.customData?.coordinates;
        for(int i = 0; i < sameBeatBurstSliders.Count; i++)
        {
            BeatmapBurstSlider c = sameBeatBurstSliders[i];
            
            if(c.x == n.x && c.y == n.y && c.c == n.c)
            {
                return sameBeatChains[i];
            }

            float[] chainCoords = c.customData?.coordinates;
            if(chainCoords != null && chainCoords.Length >= 2)
            {
                if(coordinates != null && coordinates.Length >= 2)
                {
                    if(coordinates[0].Approximately(chainCoords[0]) && coordinates[1].Approximately(chainCoords[1]))
                    {
                        return sameBeatChains[i];
                    }
                }
                else if(Mathf.RoundToInt(chainCoords[0] + 2) == n.x && Mathf.RoundToInt(chainCoords[1] + 2) == n.y)
                {
                    return sameBeatChains[i];
                }
            }
        }

        return null;
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
        bool hasDot = first.d == 8 || second.d == 8;
        if(!hasDot && (first.x == second.x || first.y == second.y))
        {
            //Don't snap notes that are on the same row or column
            //But always snap dots
            return null;
        }

        if(!(first.d == second.d || hasDot))
        {
            //Objects must have the same direction
            return null;
        }

        Vector2 firstPos = ObjectManager.CalculateObjectPosition(first.x, first.y, first.customData?.coordinates);
        Vector2 secondPos = ObjectManager.CalculateObjectPosition(second.x, second.y, second.customData?.coordinates);
        Vector2 deltaPos = firstPos - secondPos;

        float firstAngle = ObjectManager.CalculateObjectAngle(first.d);
        float secondAngle = ObjectManager.CalculateObjectAngle(second.d);
        float desiredAngle = Mathf.Atan2(-deltaPos.x, deltaPos.y) * Mathf.Rad2Deg;

        if(first.d == 8)
        {
            if(Mathf.Abs(Mathf.DeltaAngle(desiredAngle, secondAngle)) > 90)
            {
                desiredAngle = Mathf.Atan2(deltaPos.x, -deltaPos.y) * Mathf.Rad2Deg;
            }
        }
        else
        {
            if(Mathf.Abs(Mathf.DeltaAngle(desiredAngle, firstAngle)) > 90)
            {
                desiredAngle = Mathf.Atan2(deltaPos.x, -deltaPos.y) * Mathf.Rad2Deg;
            }
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


    private void Awake()
    {
        redNoteProperties = new MaterialPropertyBlock();
        blueNoteProperties = new MaterialPropertyBlock();
        redArrowProperties = new MaterialPropertyBlock();
        blueArrowProperties = new MaterialPropertyBlock();
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

    public Vector3 EndHeadPosition;

    public NoteHandler NoteHandler;
    public MaterialPropertyBlock CustomNoteProperties;
    public MaterialPropertyBlock CustomArrowProperties;


    public Note(BeatmapColorNote n)
    {
        Vector2 position = ObjectManager.CalculateObjectPosition(n.x, n.y, n.customData?.coordinates);
        float angle = ObjectManager.CalculateObjectAngle(n.d, n.a);

        Beat = n.b;
        Position = position;
        Color = n.c;
        Angle = n.customData?.angle ?? angle;
        FlipStartX = position.x;
        IsDot = n.d == 8;

        WasHit = true;
        WasBadCut = false;
        HitOffset = 0f;

        if(n.customData != null)
        {
            if(n.customData.color != null)
            {
                CustomColor = ColorManager.ColorFromCustomDataColor(n.customData.color);

                CustomNoteProperties = new MaterialPropertyBlock();
                CustomArrowProperties = new MaterialPropertyBlock();
                ObjectManager.Instance.noteManager.SetNoteMaterialProperties(ref CustomNoteProperties, ref CustomArrowProperties, (Color)CustomColor);
            }

            CustomNJS = n.customData.noteJumpMovementSpeed;
            if(n.customData.noteJumpStartBeatOffset != null)
            {
                CustomRT = BeatmapManager.GetCustomRT((float)n.customData.noteJumpStartBeatOffset);
            }
        }
    }
}