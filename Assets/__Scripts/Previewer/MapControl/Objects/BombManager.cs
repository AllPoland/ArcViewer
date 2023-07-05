using UnityEngine;

public class BombManager : MapElementManager<Bomb>
{
    [SerializeField] private ObjectPool<BombHandler> bombPool;

    [Space]
    [SerializeField] private Material complexMaterial;
    [SerializeField] private Material simpleMaterial;


    public void ReloadBombs()
    {
        ClearRenderedVisuals();
        UpdateVisuals();
    }


    public override void UpdateVisual(Bomb b)
    {
        float worldDist = objectManager.GetZPosition(b.Time);

        Vector3 worldPos = new Vector3(b.Position.x, b.Position.y, worldDist);

        if(objectManager.doMovementAnimation)
        {
            worldPos.y = objectManager.GetObjectY(b.StartY, worldPos.y, b.Time);
        }

        if(b.Visual == null)
        {
            b.BombHandler = bombPool.GetObject();
            b.Visual = b.BombHandler.gameObject;
            b.Visual.transform.SetParent(transform);
            b.Visual.SetActive(true);

            b.BombHandler.SetMaterial(objectManager.useSimpleBombMaterial ? simpleMaterial : complexMaterial);

            if(SettingsManager.GetBool("chromaobjectcolors") && b.CustomColor != null)
            {
                //This bomb has a unique chroma color
                b.BombHandler.SetProperties(b.CustomMaterialProperties);
            }
            else if(b.BombHandler.HasCustomProperties)
            {
                //This bomb has no custom color, so properties should be cleared
                b.BombHandler.ClearProperties();
            }

            RenderedObjects.Add(b);
        }
        b.Visual.transform.localPosition = worldPos;
    }


    public override bool VisualInSpawnRange(Bomb b)
    {
        return objectManager.CheckInSpawnRange(b.Time, true);
    }


    public override void ReleaseVisual(Bomb b)
    {
        bombPool.ReleaseObject(b.BombHandler);
        b.Visual = null;
        b.BombHandler = null;
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
            Bomb b = Objects[i];
            if(objectManager.CheckInSpawnRange(b.Time, true))
            {
                UpdateVisual(b);
            }
            else break;
        }
    }
}


public class Bomb : MapObject
{
    public float StartY;

    public BombHandler BombHandler;
    public MaterialPropertyBlock CustomMaterialProperties;

    public Bomb(BeatmapBombNote b)
    {
        Vector2 position = ObjectManager.CalculateObjectPosition(b.x, b.y, b.customData?.coordinates);

        Beat = b.b;
        Position = position;

        if(b.customData?.color != null)
        {
            CustomColor = ColorManager.ColorFromCustomDataColor(b.customData.color);

            CustomMaterialProperties = new MaterialPropertyBlock();
            CustomMaterialProperties.SetColor("_BaseColor", (Color)CustomColor);
        }
    }
}