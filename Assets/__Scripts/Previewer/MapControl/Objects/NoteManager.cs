using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NoteManager : MapElementManager<Note>
{
    public static Color RedNoteColor => ColorManager.CurrentColors.LeftNoteColor;
    public static Color BlueNoteColor => ColorManager.CurrentColors.RightNoteColor;

    [SerializeField] private ObjectPool<NoteHandler> notePool;

    [Header("Meshes")]
    [SerializeField] private Mesh noteMesh;
    [SerializeField] private Mesh chainHeadMesh;

    [Header("Materials")]
    [SerializeField] public Material complexMaterial;
    [SerializeField] public Material simpleMaterial;
    [SerializeField] private float noteEmission;
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


    public void ReloadNotes()
    {
        ClearRenderedVisuals();
        notePool.SetPoolSize(40);

        UpdateMaterials();
    }


    public void UpdateMaterials()
    {
        ClearRenderedVisuals();

        redNoteProperties.SetColor("_BaseColor", RedNoteColor);
        blueNoteProperties.SetColor("_BaseColor", BlueNoteColor);

        float redH, redS, redV;
        float blueH, blueS, blueV;
        Color.RGBToHSV(RedNoteColor, out redH, out redS, out redV);
        Color.RGBToHSV(BlueNoteColor, out blueH, out blueS, out blueV);

        float emission = objectManager.useSimpleNoteMaterial ? simpleNoteEmission : noteEmission;
        redNoteProperties.SetColor("_EmissionColor", RedNoteColor.SetValue(emission * redV, true));
        blueNoteProperties.SetColor("_EmissionColor", BlueNoteColor.SetValue(emission * blueV, true));

        redArrowProperties.SetColor("_BaseColor", RedNoteColor.SetHSV(null, arrowSaturation * redS, arrowBrightness * redV));
        blueArrowProperties.SetColor("_BaseColor", BlueNoteColor.SetHSV(null, arrowSaturation * blueS, arrowBrightness * blueV));

        redArrowProperties.SetColor("_EmissionColor", RedNoteColor.SetHSV(null, arrowGlowSaturation * redS, arrowEmission, true));
        blueArrowProperties.SetColor("_EmissionColor", BlueNoteColor.SetHSV(null, arrowGlowSaturation * blueS, arrowEmission, true));

        UpdateVisuals();
    }


    public override void UpdateVisual(Note n)
    {
        float reactionTime = BeatmapManager.ReactionTime;

        float worldDist = objectManager.GetZPosition(n.Time);
        Vector3 worldPos = new Vector3(n.Position.x, n.Position.y, worldDist);

        if(objectManager.doMovementAnimation)
        {
            worldPos.y = objectManager.GetObjectY(n.StartY, worldPos.y, n.Time);
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

        if(n.Visual == null)
        {
            n.noteHandler = notePool.GetObject();
            n.Visual = n.noteHandler.gameObject;

            n.Visual.transform.SetParent(transform);
            n.source = n.noteHandler.audioSource;

            n.noteHandler.SetMesh(n.IsChainHead ? chainHeadMesh : noteMesh);
            n.noteHandler.SetArrow(!n.IsDot);

            bool isRed = n.Color == 0;
            n.noteHandler.SetMaterial(objectManager.useSimpleNoteMaterial ? simpleMaterial : complexMaterial);
            n.noteHandler.SetProperties(isRed ? redNoteProperties : blueNoteProperties);
            n.noteHandler.SetArrowProperties(isRed ? redArrowProperties : blueArrowProperties);

            n.Visual.SetActive(true);
            n.noteHandler.EnableVisual();

            if(TimeManager.Playing && SettingsManager.GetFloat("hitsoundvolume") > 0)
            {
                HitSoundManager.ScheduleHitsound(n.Time, n.source);
            }

            RenderedObjects.Add(n);
        }

        n.Visual.transform.localPosition = worldPos;
        n.Visual.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }


    public override bool VisualInSpawnRange(Note n)
    {
        return objectManager.CheckInSpawnRange(n.Time);
    }


    public override void ReleaseVisual(Note n)
    {
        n.source.Stop();
        notePool.ReleaseObject(n.noteHandler);

        n.Visual = null;
        n.source = null;
        n.noteHandler = null;
    }


    public override void ClearOutsideVisuals()
    {
        for(int i = RenderedObjects.Count - 1; i >= 0; i--)
        {
            Note n = RenderedObjects[i];
            if(!objectManager.CheckInSpawnRange(n.Time))
            {
                if(n.source.isPlaying)
                {
                    //Only clear the visual elements if the hitsound is still playing
                    n.noteHandler.DisableVisual();
                    continue;
                }

                ReleaseVisual(n);
                RenderedObjects.Remove(n);
            }
            else if(!n.noteHandler.Visible)
            {
                n.noteHandler.EnableVisual();
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
            //Update each note's position
            Note n = Objects[i];
            if(objectManager.CheckInSpawnRange(n.Time))
            {
                UpdateVisual(n);
            }
            else break;
        }
    }


    public void RescheduleHitsounds()
    {
        foreach(Note n in RenderedObjects)
        {
            if(n.source != null && SettingsManager.GetFloat("hitsoundvolume") > 0)
            {
                HitSoundManager.ScheduleHitsound(n.Time, n.source);
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