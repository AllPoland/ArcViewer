using System.Collections.Generic;
using UnityEngine;

public class WallManager : MonoBehaviour
{
    [SerializeField] private ObjectPool wallPool;
    [SerializeField] private GameObject wallParent;

    [SerializeField] private Material wallMaterial;

    public List<Wall> Walls = new List<Wall>();
    public List<Wall> RenderedWalls = new List<Wall>();

    public readonly List<float> WallHeights = new List<float>
    {
        0,
        1,
        1.5f
    };

    private ObjectManager objectManager;


    public void ReloadWalls()
    {
        ClearRenderedWalls();
        wallPool.SetPoolSize(40);

        UpdateWallVisuals(TimeManager.CurrentBeat);
    }


    public void UpdateMaterial(float opacity)
    {
        Color wallColor = wallMaterial.color;
        if(wallColor.a == opacity) return;

        wallColor.a = opacity;
        wallMaterial.color = wallColor;
    }


    public void UpdateWallVisual(Wall w)
    {
        float wallStartTime = TimeManager.TimeFromBeat(w.Beat);
        float wallEndTime = TimeManager.TimeFromBeat(w.Beat + w.Duration);
        //Can't just take w.Duration for this because it doesn't work with bpm changes
        float wallDurationTime = wallEndTime - wallStartTime;
        float wallLength = objectManager.WorldSpaceFromTime(wallDurationTime);

        //Subtract 0.25 to make front face of wall line up with front face of note (walls just built like that)
        float worldDist = objectManager.GetZPosition(wallStartTime) - 0.25f;

        worldDist += wallLength / 2;

        if(w.Visual == null)
        {
            w.Visual = wallPool.GetObject();
            w.Visual.transform.SetParent(wallParent.transform);
            w.Visual.SetActive(true);

            //Wall scale only needs to be set once when it's created
            WallHandler handler = w.Visual.GetComponent<WallHandler>();
            handler.SetScale(new Vector3(w.Width, w.Height, wallLength));
            handler.SetMaterial(wallMaterial);

            RenderedWalls.Add(w);
        }
        w.Visual.transform.localPosition = new Vector3(w.Position.x, w.Position.y, worldDist);
    }


    private void ReleaseWall(Wall w)
    {
        wallPool.ReleaseObject(w.Visual);
        w.Visual = null;
    }


    public void ClearOutsideWalls()
    {
        if(RenderedWalls.Count <= 0)
        {
            return;
        }

        for(int i = RenderedWalls.Count - 1; i >= 0; i--)
        {
            Wall w = RenderedWalls[i];
            if(!objectManager.DurationObjectInSpawnRange(w.Beat, w.Beat + w.Duration))
            {
                ReleaseWall(w);
                RenderedWalls.Remove(w);
            }
        }
    }


    public void ClearRenderedWalls()
    {
        if(RenderedWalls.Count <= 0)
        {
            return;
        }

        foreach(Wall w in RenderedWalls)
        {
            ReleaseWall(w);
        }
        RenderedWalls.Clear();
    }


    public void UpdateWallVisuals(float beat)
    {
        ClearOutsideWalls();

        if(Walls.Count <= 0)
        {
            return;
        }

        int firstWall = Walls.FindIndex(x => objectManager.DurationObjectInSpawnRange(x.Beat, x.Beat + x.Duration));
        if(firstWall >= 0)
        {
            float lastBeat = 0;
            for(int i = firstWall; i < Walls.Count; i++)
            {
                //Update each wall's position
                Wall w = Walls[i];
                if(objectManager.DurationObjectInSpawnRange(w.Beat, w.Beat + w.Duration))
                {
                    UpdateWallVisual(w);
                    lastBeat = w.Beat + w.Duration;
                }
                else if(w.Duration <= w.Beat - lastBeat)
                {
                    //Continue looping if this wall overlaps in time with another
                    //This avoids edge cases where two walls that are close, with one ending before the other causes later walls to not update
                    break;
                }
            }
        }
    }


    public static float CalculateWallWidth(int width)
    {
        return width * ObjectManager.LaneWidth;
    }


    public static float CalculateWallHeight(int height)
    {
        return height * ObjectManager.WallHScale;
    }


    public static Vector2 CalculateWallPosition(int x, int y)
    {
        Vector2 position = ObjectManager.GridBottomLeft;
        position.x += x * ObjectManager.LaneWidth;
        position.y += y * ObjectManager.WallHScale;
        return position;
    }


    private void Start()
    {
        objectManager = ObjectManager.Instance;

        TimeManager.OnBeatChanged += UpdateWallVisuals;
    }
}


public class Wall : MapObject
{
    public float Duration;
    public float Width;
    public float Height;


    public static Wall WallFromBeatmapObstacle(BeatmapObstacle o)
    {
        float width = WallManager.CalculateWallWidth(o.w);
        float height = WallManager.CalculateWallHeight(o.h);
        Vector2 position = WallManager.CalculateWallPosition(o.x, o.y);
        height = Mathf.Min(height, 3f - position.y);
        position.x += (width - ObjectManager.LaneWidth) / 2;
        position.y += height / 2;

        return new Wall
        {
            Beat = o.b,
            Position = position,
            Duration = o.d,
            Width = width,
            Height = height
        };
    }
}