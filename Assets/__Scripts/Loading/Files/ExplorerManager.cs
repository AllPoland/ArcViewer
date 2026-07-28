using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SFB;
using System.IO;
using System;

public class ExplorerManager : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private MapLoader mapLoader;
    [SerializeField] private ExplorerPathLoader pathLoader;


#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);


    public void OnFileUploaded(string url)
    {
        //This is called when a file is selected by the user in WebGL
        mapLoader.LoadMapZipWebGL(url);
    }


    public void OnReplayUploaded(string url)
    {
        mapLoader.LoadReplayDirectoryWebGL(url);
    }
#endif


    public void OnPointerDown(PointerEventData eventData)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if(!ReplayManager.IsReplayMode && SettingsManager.GetInt("replaymode") > 0)
        {
            UploadFile(gameObject.name, "OnReplayUploaded", ".bsor,.dat", false);
        }
        else
        {
            UploadFile(gameObject.name, "OnFileUploaded", ".zip", false);
        }
#endif
    }


#if !UNITY_WEBGL || UNITY_EDITOR
    private void OpenFileExplorer()
    {
        ExtensionFilter[] extensions;
        if(!ReplayManager.IsReplayMode && SettingsManager.GetInt("replaymode") > 0)
        {
            extensions = new []
            {
                new ExtensionFilter("Replay Files", new string[] {"bsor", "dat"})
            };
        }
        else
        {
            extensions = new []
            {
                new ExtensionFilter("Map Files", new string[] {"zip", "dat"})
            };
        }

        string openLocation = ExplorerPathLoader.PreviousExplorerPath;
        if(openLocation == null)
        {
            openLocation = "";
        }

        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Map", openLocation, extensions, false);
        
        if(paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            try
            {
                string directory = new FileInfo(paths[0]).Directory.FullName;
                pathLoader.SetPreviousPath(directory);
            }
            catch(Exception err)
            {
                Debug.LogError($"Failed to save previous path with error: {err.Message}, {err.StackTrace}");
            }
            mapLoader.LoadMapInput(paths[0]);
        }
        else Debug.Log("No path selected!");
    }


    private void Start()
    {
        //Subscribe to the onClick event if this isn't WebGL
        GetComponent<Button>().onClick.AddListener(OpenFileExplorer);
    }
#endif
}