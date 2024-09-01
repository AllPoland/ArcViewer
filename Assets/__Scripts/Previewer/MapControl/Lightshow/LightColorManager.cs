using System.Collections.Generic;
using UnityEngine;

public class LightColorManager : MonoBehaviour
{
    private static List<Color> colors = new List<Color>();


    public static Color GetColor(int? idx)
    {
        return colors[(int)idx];
    }


    public static int GetLightColorIdx(Color color)
    {
        for(int i = 0; i < colors.Count; i++)
        {
            if(colors[i].Equals(color))
            {
                //This color is already cached, return its index
                return i;
            }
        }

        //This color has not been cached, add it to the cache and return the new index
        colors.Add(color);
        return colors.Count - 1;
    }


    private static void ClearColors()
    {
        colors.Clear();
    }


    private void UpdateDifficulty(Difficulty difficulty)
    {
        //Clear the color cache when the map is closed or changed
        ClearColors();
    }


    private void Start()
    {
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateDifficulty;
    }
}