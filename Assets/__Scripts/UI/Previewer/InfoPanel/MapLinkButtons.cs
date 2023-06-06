using UnityEngine;

public class MapLinkButtons : MonoBehaviour
{
    [SerializeField] private GameObject beatSaverButton;
    [SerializeField] private GameObject mapDownloadButton;

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
    }
}