using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInfoPanel : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI infoText;

    [Space]
    [SerializeField] private float avatarWidth;


    private void UpdateAvatar(Sprite newAvatar)
    {
        Vector2 infoTextPos = infoText.rectTransform.anchoredPosition;
        if(ReplayManager.IsReplayMode && newAvatar != null)
        {
            avatarImage.sprite = newAvatar;
            avatarImage.gameObject.SetActive(true);
            infoTextPos.x = avatarWidth;
        }
        else
        {
            avatarImage.sprite = null;
            avatarImage.gameObject.SetActive(false);
            infoTextPos.x = 0f;
        }
        infoText.rectTransform.anchoredPosition = infoTextPos;
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


    private void OnEnable()
    {
        ReplayManager.OnAvatarUpdated += UpdateAvatar;

        if(ReplayManager.IsReplayMode)
        {
            UpdateAvatar(ReplayManager.Avatar);
            UpdateInfoText();
        }
        else
        {
            avatarImage.gameObject.SetActive(false);
            infoText.gameObject.SetActive(false);
        }
    }


    private void OnDestroy()
    {
        ReplayManager.OnAvatarUpdated -= UpdateAvatar;
    }
}