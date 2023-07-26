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
    [SerializeField] private SoupInput soupInput;

    [Space]
    [SerializeField] private string placeholder;
    [SerializeField] private string webGLPlaceholder;

    [Space]
    [SerializeField] private string replayPlaceholder;
    [SerializeField] private string webGLReplayPlaceholder;

    [Space]
    [SerializeField] private string theSoupPlaceholder;

    [Space]
    public string MapDirectory;

    private TextMeshProUGUI placeholderText;


    public void LoadMap()
    {
        if(MapDirectory == "")
        {
            return;
        }

        if(soupInput.CheckSoupWord(MapDirectory))
        {
            directoryField.text = "";
            return;
        }

        if(MapDirectory.Contains(UrlArgHandler.ArcViewerURL))
        {
            //Input a shared link
            if(MapDirectory.Count(x => x == '?') == 1)
            {
                //URL contains one question mark, which means it has parameters
                string parameters = MapDirectory.Split('?').Last();
                urlArgHandler.LoadMapFromURLParameters(parameters);
                return;
            }
        }

        mapLoader.LoadMapInput(MapDirectory);
    }


    public void UpdateDirectory(string directory)
    {
        MapDirectory = directory;

        openButton.interactable = directory != "";
    }


    private void UpdatePlaceholderText()
    {
        if(SettingsManager.GetBool(TheSoup.Rule))
        {
            placeholderText.text = theSoupPlaceholder;
        }
        else if(!ReplayManager.IsReplayMode && SettingsManager.GetBool("replaymode"))
        {
#if UNITY_WEBGL
            placeholderText.text = webGLReplayPlaceholder;
#else
            placeholderText.text = replayPlaceholder;
#endif
        }
        else
        {
#if UNITY_WEBGL
            placeholderText.text = webGLPlaceholder;
#else
            placeholderText.text = placeholder;
#endif
        }
    }


    private void UpdatePlaceholderText(bool _) => UpdatePlaceholderText();


    private void UpdateReplayPrompt()
    {
        directoryField.text = "";
        UpdatePlaceholderText();
    }


    public void UpdateSettings(string changedSetting)
    {
        if(changedSetting == "all" || changedSetting == TheSoup.Rule || changedSetting == "replaymode")
        {
            UpdatePlaceholderText();
        }
    }


    private void Update()
    {
        if(Input.GetButtonDown("Submit") && EventSystem.current.currentSelectedGameObject == directoryField.gameObject)
        {
            LoadMap();
        }
    }

    private void Awake()
    {
        placeholderText = directoryField.placeholder.GetComponent<TextMeshProUGUI>();

#if UNITY_WEBGL
        placeholderText.text = webGLPlaceholder;
#endif
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;
        MapLoader.OnReplayMapPrompt += UpdateReplayPrompt;
        MapLoader.OnLoadingFailed += UpdateReplayPrompt;
        ReplayManager.OnReplayModeChanged += UpdatePlaceholderText;

        if(SettingsManager.Loaded)
        {
            UpdatePlaceholderText();
        }
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
        MapLoader.OnReplayMapPrompt -= UpdateReplayPrompt;
        MapLoader.OnLoadingFailed -= UpdateReplayPrompt;
        ReplayManager.OnReplayModeChanged -= UpdatePlaceholderText;
    }
}