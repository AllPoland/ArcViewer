using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInfoPanel : MonoBehaviour
{
    private const string userDirect = "u/";

    [SerializeField] private RawImage avatarImage;
    [SerializeField] private RectTransform infoContainer;

    [Space]
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI modifierText;

    [Space]
    [SerializeField] private Tooltip modifierTooltip;
    [SerializeField] private Button playerProfileButton;
    [SerializeField] private Button animateAvatarButton;

    private readonly Dictionary<string, string> modifierDescriptions = new Dictionary<string, string>
    {
        {"nf", "No Fail [NF] - Reaching 0 energy doesn't cause a fail"},
        {"if", "One Life (Insta-Fail) [IL] - Missing a single note causes a fail"},
        {"be", "4 Lives (Battery Energy) [BE] - Missing 4 notes causes a fail"},
        {"nb", "No Bombs [NB] - All bombs are removed from the map"},
        {"no", "No Obstacles [NO] - All walls are removed from the map"},
        {"na", "No Arrows [NA] - All arrow notes are replaced with dots"},
        {"gn", "Ghost Notes [GN] - Notes fade out entirely before reaching the player"},
        {"da", "Disappearing Arrows [DA] - Note arrows fade out before reaching the player"},
        {"sc", "Small Notes [SC] - Note size is reduced"},
        {"pm", "Pro Mode [PM] - Note hitboxes are reduced to accurate size"},
        {"sa", "Strict Angles [SA] - Swing angle ranges are less lenient"},
        {"ss", "Slower Song [SS] - The song plays at 85% speed"},
        {"fs", "Faster Song [FS] - The song plays at 120% speed"},
        {"sf", "Super Fast Song [SF] - The song plays at 150% speed"}
    };


    private void UpdateAvatar(AnimatedAvatar newAvatar)
    {
        if(ReplayManager.IsReplayMode)
        {
            avatarImage.texture = ReplayManager.AvatarRenderTexture;
            avatarImage.gameObject.SetActive(true);
            animateAvatarButton.gameObject.SetActive(newAvatar?.IsAnimated ?? false);
        }
        else
        {
            avatarImage.texture = null;
            avatarImage.gameObject.SetActive(false);
            animateAvatarButton.gameObject.SetActive(false);
        }
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


    private void UpdateModifiers()
    {
        string[] modifiers = ReplayManager.Modifiers;
        if(modifiers.Length == 0)
        {
            modifierText.gameObject.SetActive(false);
            return;
        }

        modifierText.gameObject.SetActive(true);
        modifierText.text = string.Join(", ", modifiers);
        modifierText.rectTransform.sizeDelta = modifierText.GetPreferredValues();

        string tooltip = "";
        foreach(string modifier in modifiers)
        {
            if(modifierDescriptions.TryGetValue(modifier.ToLower(), out string description))
            {
                if(tooltip == "")
                {
                    tooltip += description;
                }
                else tooltip += "<br>" + description;
            }
        }
        modifierTooltip.Text = tooltip;
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
            UpdateModifiers();
            UpdateButtons();
        }
        else
        {
            avatarImage.gameObject.SetActive(false);
            infoText.gameObject.SetActive(false);
            modifierText.gameObject.SetActive(false);
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