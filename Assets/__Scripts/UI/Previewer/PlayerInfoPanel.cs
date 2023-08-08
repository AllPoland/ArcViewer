using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInfoPanel : MonoBehaviour
{
    private const string userDirect = "u/";

    [SerializeField] private Image avatarImage;
    [SerializeField] private RectTransform infoContainer;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Button playerProfileButton;

    [Space]
    [SerializeField] private Sprite defaultImage;


    private void UpdateAvatar(Sprite newAvatar)
    {
        Vector2 infoPosition = infoContainer.anchoredPosition;
        if(ReplayManager.IsReplayMode)
        {
            if(newAvatar == null)
            {
                newAvatar = defaultImage;
            }

            avatarImage.sprite = newAvatar;
            avatarImage.gameObject.SetActive(true);
        }
        else
        {
            avatarImage.sprite = null;
            avatarImage.gameObject.SetActive(false);
        }
        infoContainer.anchoredPosition = infoPosition;
    }


    private void UpdateInfoText()
    {
        BeatleaderUser user = ReplayManager.PlayerInfo;

        string username = user?.name ?? ReplayManager.CurrentReplay?.info.playerName;
        if(!string.IsNullOrEmpty(username))
        {
            infoText.gameObject.SetActive(true);
            infoText.text = username;
        }
        else infoText.gameObject.SetActive(false);
    }


    private void UpdateButtons()
    {
        bool enableUserButton = !string.IsNullOrEmpty(ReplayManager.PlayerInfo?.id);
        playerProfileButton.gameObject.SetActive(enableUserButton);
    }


    private void OnEnable()
    {
        ReplayManager.OnAvatarUpdated += UpdateAvatar;

        if(ReplayManager.IsReplayMode)
        {
            UpdateAvatar(ReplayManager.Avatar);
            UpdateInfoText();
            UpdateButtons();
        }
        else
        {
            avatarImage.gameObject.SetActive(false);
            infoText.gameObject.SetActive(false);
            playerProfileButton.gameObject.SetActive(false);
        }
    }


    public void OpenPlayerProfile()
    {
        if(!string.IsNullOrEmpty(ReplayManager.PlayerInfo?.id))
        {
            string url = string.Concat(ReplayManager.BeatLeaderURL, userDirect, ReplayManager.PlayerInfo.id);
            Application.OpenURL(url);
        }
    }


    private void OnDestroy()
    {
        ReplayManager.OnAvatarUpdated -= UpdateAvatar;
    }
}