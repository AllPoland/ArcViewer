using System.Collections.Generic;
using UnityEngine;

public class WallManager : MonoBehaviour
{
    [SerializeField] private ObjectPool wallPool;
    [SerializeField] private GameObject wallParent;

    [SerializeField] private float wallHScale;

    public List<Wall> Walls = new List<Wall>();
    public List<Wall> RenderedWalls = new List<Wall>();

    public readonly List<float> WallHeights = new List<float>
    {
        0,
        1,
        1.5f
    };

    private ObjectManager objectManager;


    public void LoadWallsFromDifficulty(Difficulty difficulty)
    {
        ClearRenderedWalls();
        wallPool.SetPoolSize(40);

        Walls.Clear();

        BeatmapDifficulty beatmap = difficulty.beatmapDifficulty;
        if(beatmap.obstacles.Length > 0)
        {
            foreach(Obstacle o in beatmap.obstacles)
            {
                Walls.Add(Wall.WallFromObstacle(o));
            }
            Walls = ObjectManager.SortObjectsByBeat<Wall>(Walls);
        }

        UpdateWallVisuals(TimeManager.CurrentBeat);
    }


    public void UpdateWallVisual(Wall w)
    {
        float wallStartTime = TimeManager.TimeFromBeat(w.Beat);
        float wallEndTime = TimeManager.TimeFromBeat(w.Beat + w.Duration);
        //Can't just take w.Duration for this because it doesn't work with bpm changes
        float wallDurationTime = wallEndTime - wallStartTime;
        float wallLength = objectManager.WorldSpaceFromTime(wallDurationTime);

        float laneWidth = objectManager.laneWidth;

        float width = w.Width * laneWidth;

        Vector2 gridPos = objectManager.bottomLeft;
        gridPos.x += w.x * laneWidth;

        float correctionX = laneWidth * (w.Width - 1) / 2;
        float worldX = gridPos.x + correctionX;

        float wallY = Mathf.Clamp(w.y, 0, 2);
        float worldY = wallY * wallHScale;

        float wallHeight = Mathf.Min(w.Height, 5 - wallY);
        float worldHeight = wallHeight * wallHScale;

        worldY += worldHeight / 2;

        //Subtract 0.25 to make front face of wall line up with front face of note (walls just built like that)
        float worldDist = objectManager.GetZPosition(wallStartTime) - 0.25f;

        worldDist += wallLength / 2;

        if(w.Visual == null)
        {
            w.Visual = wallPool.GetObject();
            w.Visual.transform.SetParent(wallParent.transform);
            w.Visual.SetActive(true);

            //Wall scale only needs to be set once when it's created
            WallScaleHandler scaleHandler = w.Visual.GetComponent<WallScaleHandler>();
            scaleHandler.SetScale(new Vector3(width, worldHeight, wallLength));

            RenderedWalls.Add(w);
        }
        w.Visual.transform.localPosition = new Vector3(worldX, worldY, worldDist);
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


    private void Start()
    {
        objectManager = ObjectManager.Instance;

        TimeManager.OnBeatChanged += UpdateWallVisuals;
    }
}