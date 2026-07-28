using UnityEngine;
using UnityEngine.UI;

public class MapLinkButtons : MonoBehaviour
{
    [SerializeField] private GameObject shareButton;
    [SerializeField] private GameObject beatSaverButton;
    [SerializeField] private GameObject mapDownloadButton;
    [SerializeField] private GameObject leaderboardButton;

    [Space]
    [SerializeField] private Image leaderboardIcon;
    [SerializeField] private Tooltip leaderboardTooltip;
    [SerializeField] private Sprite beatLeaderIcon;
    [SerializeField] private Sprite scoreSaberIcon;

    private const string beatSaverURL = "https://beatsaver.com/";
    private const string mapDirect = "maps/";
    
    public void OpenBeatSaverLink()
    {
        if(string.IsNullOrEmpty(UrlArgHandler.LoadedMapID))
        {
            Debug.LogWarning("Tried to open a BeatSaver link with no map ID!");
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Map has no BeatSaver ID!");
            beatSaverButton.SetActive(false);
            return;
        }

        string mapURL = string.Concat(beatSaverURL, mapDirect, UrlArgHandler.LoadedMapID);
        Application.OpenURL(mapURL);
    }


    public void OpenDownloadLink()
    {
        if(string.IsNullOrEmpty(UrlArgHandler.LoadedMapURL))
        {
            Debug.LogWarning("Tried to open a download link with no URL!");
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Map has no download URL!");
            mapDownloadButton.SetActive(false);
            return;
        }

        Application.OpenURL(UrlArgHandler.LoadedMapURL);
    }


    public void OpenLeaderboardLink()
    {
        ReplaySourceInfo source = ReplayManager.SourceInfo;
        if(source == null || !source.HasLeaderboard)
        {
            Debug.LogWarning("Tried to open a leaderboard link with no URL!");
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Replay has no leaderboard URL!");
            leaderboardButton.SetActive(false);
            return;
        }

        Application.OpenURL(source.LeaderboardURL);
    }


    private void UpdateLeaderboardAppearance(ReplaySourceInfo source)
    {
        if(leaderboardIcon != null)
        {
            leaderboardIcon.sprite = source.SourceType switch
            {
                ReplaySourceType.ScoreSaber => scoreSaberIcon,
                _ => beatLeaderIcon
            };
        }

        if(leaderboardTooltip != null)
        {
            leaderboardTooltip.Text = string.IsNullOrEmpty(source.SourceName)
                ? "Open this map's leaderboard page"
                : $"Open this map's {source.SourceName} leaderboard page";
        }
    }


    private void UpdateShareButton()
    {
        bool enable = ReplayManager.IsReplayMode
            ? !string.IsNullOrEmpty(UrlArgHandler.LoadedBLReplayID) || !string.IsNullOrEmpty(UrlArgHandler.LoadedReplayURL) || !string.IsNullOrEmpty(UrlArgHandler.LoadedSSScoreId)
            : !string.IsNullOrEmpty(UrlArgHandler.LoadedMapID) || !string.IsNullOrEmpty(UrlArgHandler.LoadedMapURL);
        shareButton.SetActive(enable);
    }


    private void OnEnable()
    {
        bool hasMapID = !string.IsNullOrEmpty(UrlArgHandler.LoadedMapID);
        beatSaverButton.SetActive(hasMapID);
        mapDownloadButton.SetActive(!hasMapID && !string.IsNullOrEmpty(UrlArgHandler.LoadedMapURL));

        bool hasLeaderboard = ReplayManager.SourceInfo?.HasLeaderboard ?? false;
        leaderboardButton.SetActive(hasLeaderboard);
        if(hasLeaderboard)
        {
            UpdateLeaderboardAppearance(ReplayManager.SourceInfo);
        }
        UpdateShareButton();
    }
}