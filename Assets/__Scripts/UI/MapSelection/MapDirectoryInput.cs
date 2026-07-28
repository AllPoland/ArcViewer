using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MapDirectoryInput : MonoBehaviour
{
    private const string ScoreSaberHost = "watch.scoresaber.com";

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


    private string CombineArgument(string name, string value)
    {
        return string.Join('=', name, value);
    }


    private List<string> ConvertBeatLeaderViewerParameters(List<KeyValuePair<string, string>> parameters)
    {
        List<string> convertedArgs = new List<string>();
        foreach(KeyValuePair<string, string> pair in parameters)
        {
            string name = pair.Key;
            string value = pair.Value;

            string newName;
            switch(name)
            {
                case "scoreId":
                    newName = "scoreID";
                    convertedArgs.Add(CombineArgument(newName, value));
                    break;
                case "link":
                    newName = "replayURL";
                    convertedArgs.Add(CombineArgument(newName, value));
                    break;
                case "mapLink":
                    newName = "url";
                    convertedArgs.Add(CombineArgument(newName, value));
                    break;
                case "time":
                    newName = "t";
                    if(int.TryParse(value, out int result))
                    {
                        //BL stores timestamps in ms, while ArcViewer uses seconds
                        value = ((float)result / 1000).ToString();
                        convertedArgs.Add(CombineArgument(newName, value));
                    }
                    break;
            }
        }
        return convertedArgs;
    }


    private static string GetQueryValue(List<KeyValuePair<string, string>> parameters, params string[] names)
    {
        foreach(string name in names)
        {
            KeyValuePair<string, string> match = parameters.FirstOrDefault(x => x.Key == name);
            if(!string.IsNullOrEmpty(match.Value))
            {
                return match.Value;
            }
        }

        return null;
    }


    private static string GetScoreSaberScoreID(Uri uri)
    {
        string[] segments = uri.AbsolutePath.Split('/').Where(x => !string.IsNullOrEmpty(x)).ToArray();
        for(int i = 0; i < segments.Length - 1; i++)
        {
            if(!segments[i].Equals("scores", System.StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }

            string scoreID = segments[i + 1];
            if(scoreID.All(char.IsDigit))
            {
                return scoreID;
            }
        }

        return null;
    }


    private bool TryLoadScoreSaberURL(string rawUrl)
    {
        if(!Uri.TryCreate(HttpUtility.UrlDecode(rawUrl), UriKind.Absolute, out Uri uri))
        {
            return false;
        }

        string host = uri.Host;
        if(host.StartsWith("www.", StringComparison.InvariantCultureIgnoreCase))
        {
            host = host[4..];
        }
        if(!host.Equals(ScoreSaberHost, System.StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
        }

        List<KeyValuePair<string, string>> parameters = UrlUtility.ParseUrlParams(uri.ToString());
        string scoreID = GetQueryValue(parameters, "ssScoreId", "ssScoreID", "scoreID", "scoreId");
        if(string.IsNullOrEmpty(scoreID))
        {
            scoreID = GetScoreSaberScoreID(uri);
        }

        if(string.IsNullOrEmpty(scoreID) || !scoreID.All(char.IsDigit))
        {
            return false;
        }

        List<string> convertedArgs = new List<string> { CombineArgument("ssScoreId", scoreID) };

        string time = GetQueryValue(parameters, "t", "time");
        if(!string.IsNullOrEmpty(time))
        {
            convertedArgs.Add(CombineArgument("t", time));
        }

        string mapID = GetQueryValue(parameters, "id");
        if(!string.IsNullOrEmpty(mapID))
        {
            convertedArgs.Add(CombineArgument("id", mapID));
        }

        string mapURL = GetQueryValue(parameters, "url");
        if(!string.IsNullOrEmpty(mapURL))
        {
            convertedArgs.Add(CombineArgument("url", mapURL));
        }

        string newQuery = string.Join('&', convertedArgs);
        urlArgHandler.LoadMapFromShareableURL($"{UrlArgHandler.ArcViewerURL}?{newQuery}");
        return true;
    }


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

        if(MapDirectory.StartsWith(UrlArgHandler.ArcViewerURL))
        {
            //Input a shared link
            urlArgHandler.LoadMapFromShareableURL(MapDirectory);
            return;
        }

        if(MapDirectory.StartsWith(UrlArgHandler.BeatLeaderViewerURL) || MapDirectory.StartsWith(UrlArgHandler.OldBeatLeaderViewerURL))
        {
            //Convert BeatLeader viewer links to ArcViewer parameters
            string url = HttpUtility.UrlDecode(MapDirectory);

            List<KeyValuePair<string, string>> parameters = UrlUtility.ParseUrlParams(url);
            List<string> convertedArgs = ConvertBeatLeaderViewerParameters(parameters);

            if(convertedArgs.Count > 0)
            {
                string newQuery = string.Join('&', convertedArgs);
                urlArgHandler.LoadMapFromShareableURL($"{UrlArgHandler.ArcViewerURL}?{newQuery}");
                return;
            }
            else
            {
                Debug.LogWarning($"Invalid sharing URL: {MapDirectory}");
                ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Invalid sharing URL!");
                return;
            }
        }

        if(TryLoadScoreSaberURL(MapDirectory))
        {
            return;
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
        else if(!ReplayManager.IsReplayMode && SettingsManager.GetInt("replaymode") > 0)
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