using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MapDirectoryInput : MonoBehaviour
{
    [SerializeField] private MapLoader mapLoader;
    [SerializeField] private TMP_InputField directoryField;
    [SerializeField] private Button openButton;

    [SerializeField] private string webGLPlaceholder;

    public string MapDirectory;


    public void LoadMap()
    {
        if(MapDirectory != "")
        {
            mapLoader.LoadMapDirectory(MapDirectory);
        }
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


    private void Start()
    {
        if(Application.platform == RuntimePlatform.WebGLPlayer)
        {
            directoryField.placeholder.GetComponent<TextMeshProUGUI>().text = webGLPlaceholder;
        }
    }
}