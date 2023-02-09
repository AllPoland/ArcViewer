using System.Collections;
using System.Collections.Generic;
using System.IO;
using SFB;
using UnityEngine;

public class ExplorerManager : MonoBehaviour
{
    [SerializeField] private MapLoader mapLoader;

    public void OpenFileExplorer()
    {
        var extensions = new []
        {
            new ExtensionFilter("Compressed (zip) Folders", "zip"),
            new ExtensionFilter("All Files", ("*"))
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Map", "", extensions, false);
        
        if(paths.Length > 0)
        {
            mapLoader.LoadMapDirectory(paths[0]);
        }
        else Debug.Log("No path selected!");
    }
}