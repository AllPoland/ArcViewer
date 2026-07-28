using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInfoPanel : MonoBehaviour
{
    [SerializeField] private RawImage avatarImage;
    [SerializeField] private RectTransform infoContainer;

    [Space]
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI modifierText;

    [Space]
    [SerializeField] private Tooltip modifierTooltip;
    [SerializeField] private Button playerProfileButton;
    [SerializeField] private Tooltip playerProfileTooltip;
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
        string username = ReplayManager.SourceInfo?.PlayerName
            ?? ReplayManager.CurrentReplay?.info.playerName;

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

        modifierTooltip.Text = string.Join("<br>", modifiers
            .Select(m =>
            {
                string description;
                return modifierDescriptions.TryGetValue(m.ToLower(), out description) ? description : null;
            })
            .Where(d => d != null));
    }


    private void UpdateButtons()
    {
        bool enableUserButton = ReplayManager.SourceInfo?.HasPlayerProfile ?? false;
        playerProfileButton.gameObject.SetActive(enableUserButton);

        if(enableUserButton && playerProfileTooltip != null)
        {
            string sourceName = ReplayManager.SourceInfo.SourceName;
            playerProfileTooltip.Text = string.IsNullOrEmpty(sourceName)
                ? "Open this player's profile"
                : $"Open this player's {sourceName} profile";
        }
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
        ReplaySourceInfo source = ReplayManager.SourceInfo;
        if(source != null && source.HasPlayerProfile)
        {
            Application.OpenURL(source.PlayerProfileURL);
        }
    }


    private void OnDestroy()
    {
        ReplayManager.OnAvatarUpdated -= UpdateAvatar;
    }
}