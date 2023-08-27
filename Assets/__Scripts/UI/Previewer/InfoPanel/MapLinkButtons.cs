using UnityEngine;

public class MapLinkButtons : MonoBehaviour
{
    [SerializeField] private GameObject shareButton;
    [SerializeField] private GameObject beatSaverButton;
    [SerializeField] private GameObject mapDownloadButton;
    [SerializeField] private GameObject leaderboardButton;

    private const string beatSaverURL = "https://beatsaver.com/";
    private const string mapDirect = "maps/";
    
    private const string leaderboardDirect = "leaderboard/global/";


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
        if(string.IsNullOrEmpty(ReplayManager.LeaderboardID))
        {
            Debug.LogWarning("Tried to open a leaderboard link with no URL!");
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Replay has no leaderboard URL!");
            leaderboardButton.SetActive(false);
            return;
        }

        string leaderboardURl = string.Concat(ReplayManager.BeatLeaderURL, leaderboardDirect, ReplayManager.LeaderboardID);
        Application.OpenURL(leaderboardURl);
    }


    private void UpdateShareButton()
    {
        if(ReplayManager.IsReplayMode)
        {
            bool enable = !string.IsNullOrEmpty(UrlArgHandler.LoadedReplayID) || !string.IsNullOrEmpty(UrlArgHandler.LoadedReplayURL);
            shareButton.SetActive(enable);
        }
        else
        {
            bool enable = !string.IsNullOrEmpty(UrlArgHandler.LoadedMapID) || !string.IsNullOrEmpty(UrlArgHandler.LoadedMapURL);
            shareButton.SetActive(enable);
        }
    }


    private void OnEnable()
    {
        beatSaverButton.SetActive(false);
        mapDownloadButton.SetActive(false);

        if(!string.IsNullOrEmpty(UrlArgHandler.LoadedMapID))
        {
            beatSaverButton.SetActive(true);
        }
        else if(!string.IsNullOrEmpty(UrlArgHandler.LoadedMapURL))
        {
            mapDownloadButton.SetActive(true);
        }

        leaderboardButton.SetActive(!string.IsNullOrEmpty(ReplayManager.LeaderboardID));
        UpdateShareButton();
    }
}