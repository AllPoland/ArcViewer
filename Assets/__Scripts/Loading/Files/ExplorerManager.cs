using System.Collections;
using System.Collections.Generic;
using System.IO;
using SFB;
using UnityEngine;

public class ExplorerManager : MonoBehaviour
{
    [SerializeField] private BeatmapLoader beatmapLoader;

    public void OpenFileExplorer()
    {
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Map", "", "", false);
        
        if(paths.Length > 0)
        {
            beatmapLoader.LoadMapDirectory(paths[0]);
        }
        else Debug.Log("No path selected!");
    }
}