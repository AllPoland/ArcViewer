using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SFB;

public class ExplorerManager : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private MapLoader mapLoader;


#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);


    public void OnFileUploaded(string url)
    {
        //This is called when a file is selected by the user in WebGL
        mapLoader.LoadMapZipWebGL(url);
    }
#endif


    public void OnPointerDown(PointerEventData eventData)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UploadFile(gameObject.name, "OnFileUploaded", ".zip", false);
#endif
    }


    public void OpenFileExplorer()
    {
        ExtensionFilter[] extensions = new []
        {
            new ExtensionFilter("Map and Replay Files", new string[] {"zip", "dat", "bsor"})
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Map", "", extensions, false);
        
        if(paths.Length > 0)
        {
            mapLoader.LoadMapInput(paths[0]);
        }
        else Debug.Log("No path selected!");
    }


#if !UNITY_WEBGL || UNITY_EDITOR
    private void Start()
    {
        //Subscribe to the onClick event if this isn't WebGL
        GetComponent<Button>().onClick.AddListener(OpenFileExplorer);
    }
#endif
}