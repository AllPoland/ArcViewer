using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallManager : MonoBehaviour
{
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject wallParent;

    public List<Wall> Walls = new List<Wall>();
    public List<Wall> RenderedWalls = new List<Wall>();

    public readonly List<float> WallHeights = new List<float>
    {
        0,
        1,
        1.5f
    };

    private TimeManager timeManager;
    private ObjectManager objectManager;
    private BeatmapManager beatmapManager;


    public void LoadWallsFromDifficulty(Difficulty difficulty)
    {
        ClearRenderedWalls();
        BeatmapDifficulty beatmap = difficulty.beatmapDifficulty;

        if(beatmap.obstacles.Length > 0)
        {
            List<Wall> newWallList = new List<Wall>();
            foreach(Obstacle o in beatmap.obstacles)
            {
                newWallList.Add( Wall.WallFromObstacle(o));
            }

            Walls = ObjectManager.SortObjectsByBeat<Wall>(newWallList);
        }
        else Walls = new List<Wall>();

        UpdateWallVisuals(timeManager.CurrentBeat);
    }


    public void UpdateWallVisual(Wall w)
    {
        float wallStartTime = TimeManager.TimeFromBeat(w.Beat);
        float wallDurationTime = TimeManager.TimeFromBeat(w.Duration);
        float wallLength = objectManager.WorldSpaceFromTime(wallDurationTime);

        float laneWidth = objectManager.laneWidth;
        float rowHeight = objectManager.rowHeight * 0.9f;

        float width = w.Width * laneWidth;

        Vector2 gridPos = objectManager.bottomLeft;
        gridPos.x += w.x * laneWidth;

        float correctionX = laneWidth * (w.Width - 1) / 2;
        float worldX = gridPos.x + correctionX;

        float useY = w.y;
        if(useY < 0)
        {
            useY = 0;
        }
        else if(useY >= 2)
        {
            useY = 1.5f;
        }
        else
        {
            useY = 0.5f;
        }

        float worldY = useY * rowHeight;
        float height = w.Height * rowHeight;

        float correctionY = (height - objectManager.rowHeight) / 2;
        worldY += correctionY;

        //I'll need to lean how walls actually work so I don't have to manually handle these things...
        if(w.y == 0)
        {
            worldY -= 0.3f;
        }
        else if(w.y == 2)
        {
            worldY += 0.2f;
        }

        float worldDist = objectManager.GetZPosition(wallStartTime);

        worldDist += wallLength / 2;

        if(w.Visual == null)
        {
            w.Visual = Instantiate(wallPrefab);
            w.Visual.transform.SetParent(wallParent.transform);

            RenderedWalls.Add(w);
        }
        w.Visual.transform.localPosition = new Vector3(worldX, worldY, worldDist);
        w.Visual.transform.localScale = new Vector3(width, height, wallLength);
    }


    public void ClearOutsideWalls()
    {
        if(RenderedWalls.Count > 0)
        {
            List<Wall> removeWalls = new List<Wall>();
            foreach(Wall w in RenderedWalls)
            {
                if(!CheckWallInSpawnRange(w))
                {
                    w.ClearVisual();
                    removeWalls.Add(w);
                }
            }

            foreach(Wall w in removeWalls)
            {
                RenderedWalls.Remove(w);
            }
        }
    }


    public void ClearRenderedWalls()
    {
        if(RenderedWalls.Count > 0)
        {
            foreach(Wall w in RenderedWalls)
            {
                w.ClearVisual();
            }
            RenderedWalls.Clear();
        }
    }


    public void UpdateWallVisuals(float beat)
    {
        ClearOutsideWalls();

        if(Walls.Count > 0)
        {
            int firstWall = Walls.FindIndex(x => CheckWallInSpawnRange(x));
            if(firstWall < 0)
            {
                //Debug.Log("No more walls.");
            }
            else
            {
                float lastBeat = 0f;
                for(int i = firstWall; i < Walls.Count; i++)
                {
                    //Update each wall's position
                    Wall w = Walls[i];
                    if(CheckWallInSpawnRange(w))
                    {
                        UpdateWallVisual(w);
                        lastBeat = w.Beat + w.Duration;
                    }
                    else if(w.Duration <= w.Beat - lastBeat)
                    {
                        //Continue looping if this wall overlaps in time with another
                        //This avoids edge cases where two walls that are close, with one ending before the other causing later walls to not update
                        break;
                    }
                }
            }
        }
    }


    public bool CheckWallInSpawnRange(Wall w)
    {
        float wallStartTime = TimeManager.TimeFromBeat(w.Beat);
        float wallEndBeat = w.Beat + w.Duration;
        float wallEndTime = TimeManager.TimeFromBeat(wallEndBeat) - objectManager.BehindCameraTime;

        bool timeInRange = timeManager.CurrentTime > wallStartTime && timeManager.CurrentTime <= wallEndTime;
        bool jumpTime = objectManager.CheckInSpawnRange(w.Beat);

        return jumpTime || timeInRange;
    }


    private void Start()
    {
        timeManager = TimeManager.Instance;
        objectManager = ObjectManager.Instance;
        beatmapManager = BeatmapManager.Instance;

        if(beatmapManager != null)
        {
            beatmapManager.OnBeatmapDifficultyChanged += LoadWallsFromDifficulty;
        }
        if(timeManager != null)
        {
            timeManager.OnBeatChanged += UpdateWallVisuals;
        }
    }
}