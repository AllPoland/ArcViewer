using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallManager : MonoBehaviour
{
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject wallParent;

    [SerializeField] private float wallHeightOffset;

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

        UpdateWallVisuals(TimeManager.CurrentBeat);
    }


    public void UpdateWallVisual(Wall w)
    {
        float wallStartTime = TimeManager.TimeFromBeat(w.Beat);
        float wallEndTime = TimeManager.TimeFromBeat(w.Beat + w.Duration);
        float wallDurationTime = wallEndTime - wallStartTime;
        float wallLength = objectManager.WorldSpaceFromTime(wallDurationTime);

        float laneWidth = objectManager.laneWidth;
        float rowHeight = objectManager.rowHeight;

        float width = w.Width * laneWidth;

        Vector2 gridPos = objectManager.bottomLeft;
        gridPos.x += w.x * laneWidth;

        float correctionX = laneWidth * (w.Width - 1) / 2;
        float worldX = gridPos.x + correctionX;

        float wallY = Mathf.Clamp(w.y, 0, 2);
        float worldY = wallY * rowHeight;

        float maxHeight = 5 - wallY;
        float wallHeight = Mathf.Min(w.Height, maxHeight);
        float worldHeight = wallHeight * rowHeight;

        worldY += worldHeight / 2;
        worldY += wallHeightOffset;

        float worldDist = objectManager.GetZPosition(wallStartTime);

        worldDist += wallLength / 2;

        if(w.Visual == null)
        {
            w.Visual = Instantiate(wallPrefab);
            w.Visual.transform.SetParent(wallParent.transform);

            //Wall scale only needs to be set once when it's created
            WallScaleHandler scaleHandler = w.Visual.GetComponent<WallScaleHandler>();
            scaleHandler.SetScale(new Vector3(width, worldHeight, wallLength));

            RenderedWalls.Add(w);
        }
        w.Visual.transform.localPosition = new Vector3(worldX, worldY, worldDist);
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
                        //This avoids edge cases where two walls that are close, with one ending before the other causes later walls to not update
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

        bool timeInRange = TimeManager.CurrentTime > wallStartTime && TimeManager.CurrentTime <= wallEndTime;
        bool jumpTime = objectManager.CheckInSpawnRange(w.Beat);

        return jumpTime || timeInRange;
    }


    private void Start()
    {
        objectManager = ObjectManager.Instance;

        BeatmapManager.OnBeatmapDifficultyChanged += LoadWallsFromDifficulty;
        TimeManager.OnBeatChanged += UpdateWallVisuals;
    }
}