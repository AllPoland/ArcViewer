using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDirectoryInput : MonoBehaviour
{
    [SerializeField] private BeatmapLoader beatmapLoader;

    public string MapDirectory;


    public void LoadMap()
    {
        beatmapLoader.LoadMapDirectory(MapDirectory);
    }


    public void UpdateDirectory(string directory)
    {
        MapDirectory = directory;
    }
}