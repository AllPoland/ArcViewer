using UnityEngine;

public class WallManager : MapElementManager<Wall>
{
    public static Color WallColor => ColorManager.CurrentColors.WallColor;

    [Header("Components")]
    [SerializeField] private ObjectPool<WallHandler> wallPool;
    [SerializeField] private GameObject wallParent;

    [Header("Parameters")]
    [SerializeField, Range(0f, 1f)] private float wallLightness;
    [SerializeField] private float wallEmission;
    [SerializeField, Range(0f, 1f)] private float edgeSaturation;
    [SerializeField] private float edgeEmission;

    private MaterialPropertyBlock wallMaterialProperties;
    private MaterialPropertyBlock wallEdgeProperties;


    public void ReloadWalls()
    {
        ClearRenderedVisuals();
        wallPool.SetPoolSize(40);

        UpdateVisuals();
    }


    public void UpdateMaterial()
    {
        ClearRenderedVisuals();

        float h, s, v;
        Color.RGBToHSV(WallColor, out h, out s, out v);

        Color newColor = WallColor.SetValue(wallLightness * v);
        newColor.a = SettingsManager.GetFloat("wallopacity");
        wallMaterialProperties.SetColor("_BaseColor", newColor);
        wallMaterialProperties.SetColor("_EmissionColor", newColor.SetValue(wallEmission));

        wallEdgeProperties.SetColor("_BaseColor", newColor.SetSaturation(s * edgeSaturation));
        wallEdgeProperties.SetColor("_EmissionColor", newColor.SetHSV(h, s * edgeSaturation, edgeEmission, true));

        UpdateVisuals();
    }


    public override void UpdateVisual(Wall w)
    {
        float wallLength = objectManager.WorldSpaceFromTime(w.DurationTime);

        //Subtract 0.25 to make front face of wall line up with front face of note (walls just built like that)
        float worldDist = objectManager.GetZPosition(w.Time) - 0.25f;

        worldDist += wallLength / 2;

        if(w.Visual == null)
        {
            w.wallHandler = wallPool.GetObject();
            w.Visual = w.wallHandler.gameObject;

            w.Visual.transform.SetParent(wallParent.transform);
            w.Visual.SetActive(true);

            w.wallHandler.SetScale(new Vector3(w.Width, w.Height, wallLength));
            w.wallHandler.SetProperties(wallMaterialProperties);
            w.wallHandler.SetEdgeProperties(wallEdgeProperties);

            RenderedObjects.Add(w);
        }
        w.Visual.transform.localPosition = new Vector3(w.Position.x, w.Position.y, worldDist);
    }


    public override bool VisualInSpawnRange(Wall w)
    {
        return objectManager.DurationObjectInSpawnRange(w.Time, w.Time + w.DurationTime);
    }


    public override void ReleaseVisual(Wall w)
    {
        wallPool.ReleaseObject(w.wallHandler);
        w.Visual = null;
        w.wallHandler = null;
    }


    public override void UpdateVisuals()
    {
        ClearOutsideVisuals();

        if(Objects.Count == 0)
        {
            return;
        }

        float lastBeat = 0;
        for(int i = GetStartIndex(TimeManager.CurrentTime); i < Objects.Count; i++)
        {
            //Update each wall's position
            Wall w = Objects[i];
            if(objectManager.DurationObjectInSpawnRange(w.Time, w.Time + w.DurationTime))
            {
                UpdateVisual(w);
                lastBeat = w.Beat + w.DurationBeats;
            }
            else if(w.DurationBeats <= w.Beat - lastBeat)
            {
                //Continue looping if this wall overlaps in time with another
                //This avoids edge cases where two walls that overlap,
                //with one starting and ending before the other, causes later walls to not update
                break;
            }
        }
    }


    public static float CalculateWallWidth(float width)
    {
        if(BeatmapManager.MappingExtensions)
        {
            width = ObjectManager.MappingExtensionsPrecision(width);
        }

        return width * ObjectManager.LaneWidth;
    }


    public static float CalculateWallHeight(float height)
    {
        if(BeatmapManager.MappingExtensions)
        {
            height = ObjectManager.MappingExtensionsPrecision(height);
        }

        return height * ObjectManager.WallHScale;
    }


    public static Vector2 CalculateWallPosition(float x, float y, float[] coordinates = null)
    {
        if(coordinates != null && coordinates.Length >= 2)
        {
            //Noodle coordinates treat x differently for some reason
            x = coordinates[0] + 2;
            y = coordinates[1];
        }
        else if(BeatmapManager.MappingExtensions)
        {
            Vector2 adjustedPosition = ObjectManager.MappingExtensionsPosition(new Vector2(x, y));
            x = adjustedPosition.x;
            y = adjustedPosition.y;
        }

        Vector2 position = ObjectManager.GridBottomLeft;
        position.x += x * ObjectManager.LaneWidth;
        position.y += y * ObjectManager.WallHScale;
        return position;
    }


    private void Awake()
    {
        wallMaterialProperties = new MaterialPropertyBlock();
        wallEdgeProperties = new MaterialPropertyBlock();
    }
}


public class Wall : MapObject
{
    private float _durationBeats;
    public float DurationBeats
    {
        get => _durationBeats;
        set
        {
            _durationBeats = value;

            //Can't just directly convert durationBeats because that doesn't work with bpm changes
            float endTime = TimeManager.TimeFromBeat(Beat + _durationBeats);
            DurationTime = endTime - Time;
        }
    }
    public float DurationTime { get; private set; }

    public float Width;
    public float Height;
    public WallHandler wallHandler;


    public static Wall WallFromBeatmapObstacle(BeatmapObstacle o)
    {
        float width = o.w;
        float height = o.h;
        if(o.customData?.size != null && o.customData.size.Length > 0)
        {
            float[] size = o.customData.size;
            width = size[0];
            if(size.Length > 1)
            {
                height = size[1];
            }
        }

        int y = o.y;
        if(!BeatmapManager.MappingExtensions && !BeatmapManager.NoodleExtensions)
        {
            //Height and y are capped in vanilla
            y = Mathf.Clamp(y, 0, 2);
            height = Mathf.Min(height, 5f - y);
        }

        float worldWidth = WallManager.CalculateWallWidth(width);
        float worldHeight = WallManager.CalculateWallHeight(height);

        Vector2 position = WallManager.CalculateWallPosition(o.x, y, o.customData?.coordinates);
        position.x += (worldWidth - ObjectManager.LaneWidth) / 2;
        position.y += worldHeight / 2;

        float beat = o.b;
        float duration = o.d;
        if(duration < 0)
        {
            //Negative duration walls break stuff, flip the start and end so they act like regular walls
            beat = beat + duration;
            duration = -duration;
        }

        return new Wall
        {
            Beat = beat,
            Position = position,
            DurationBeats = duration,
            Width = worldWidth,
            Height = worldHeight
        };
    }
}