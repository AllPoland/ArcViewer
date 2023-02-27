using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MapDirectoryInput : MonoBehaviour
{
    [SerializeField] private MapLoader mapLoader;
    [SerializeField] private UrlArgHandler urlArgHandler;
    [SerializeField] private TMP_InputField directoryField;
    [SerializeField] private Button openButton;

    [SerializeField] private string webGLPlaceholder;

    public string MapDirectory;


    public void LoadMap()
    {
        if(MapDirectory == "") return;

        if(MapDirectory.Contains(UrlArgHandler.ArcViewerURL))
        {
            //Input a shared link
            if(MapDirectory.Count(x => x == '?') == 1)
            {
                //URL contains one question mark, which means it has parameters
                string parameters = MapDirectory.Split('?').Last();
                urlArgHandler.LoadMapFromParameters(parameters);
                return;
            }
        }

        mapLoader.LoadMapDirectory(MapDirectory);
    }


    public void UpdateDirectory(string directory)
    {
        MapDirectory = directory;

        openButton.interactable = directory != "";
    }


    private void Update()
    {
        if(Input.GetButtonDown("Submit") && EventSystem.current.currentSelectedGameObject == directoryField.gameObject)
        {
            LoadMap();
        }
    }

#if UNITY_WEBGL
    private void Start()
    {
        directoryField.placeholder.GetComponent<TextMeshProUGUI>().text = webGLPlaceholder;
    }
#endif
}