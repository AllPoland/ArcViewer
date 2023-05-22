using UnityEngine;

public class BombManager : MapElementManager<Bomb>
{
    [SerializeField] private ObjectPool bombPool;


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
            b.Visual = bombPool.GetObject();
            b.Visual.transform.SetParent(transform);
            b.Visual.SetActive(true);

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
        bombPool.ReleaseObject(b.Visual);
        b.Visual = null;
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

    public static Bomb BombFromBeatmapBombNote(BeatmapBombNote b)
    {
        Vector2 position = ObjectManager.CalculateObjectPosition(b.x, b.y, b.customData?.coordinates);

        return new Bomb
        {
            Beat = b.b,
            Position = position
        };
    }
}