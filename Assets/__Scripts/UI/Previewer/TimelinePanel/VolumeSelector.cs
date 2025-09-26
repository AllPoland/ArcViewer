using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class VolumeSelector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject sliderContainer;

    [Space]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private TMP_InputField musicInputField;
    [SerializeField] private Slider hitsoundSlider;
    [SerializeField] private TMP_InputField hitsoundInputField;

    [Space]
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color highlightedColor;

    [Space]
    [SerializeField] private Image mainImage;
    [SerializeField] private Image musicImage;
    [SerializeField] private Image hitsoundImage;

    [Space]
    [SerializeField] private Sprite mainActiveSprite;
    [SerializeField] private Sprite mainMutedSprite;
    [SerializeField] private Sprite musicActiveSprite;
    [SerializeField] private Sprite musicMutedSprite;
    [SerializeField] private Sprite hitsoundActiveSprite;
    [SerializeField] private Sprite hitsoundMutedSprite;


    private bool hovered;
    private bool clicked;
    private bool musicTextSelected;
    private bool hitsoundTextSelected;
    private bool muted;
    private bool musicMuted;
    private bool hitsoundMuted;
    private float savedMusicVolume;
    private float savedHitsoundVolume;

    private const float sliderStepScale = 0.05f;
    private const string musicRule = "musicvolume";
    private const string hitsoundRule = "hitsoundvolume";


    public void ShowSliders()
    {
        mainImage.color = highlightedColor;

        sliderContainer.SetActive(true);
        UpdateValueDisplay();
    }


    public void HideSliders()
    {
        //Don't hide the slider if the mouse is over us, or if we're still clicked on
        if(hovered || clicked || musicTextSelected || hitsoundTextSelected) return;

        mainImage.color = defaultColor;

        sliderContainer.SetActive(false);
    }


    public void OnMuteButtonClick()
    {
        if(muted)
        {
            SetMusicVolume(savedMusicVolume);
            SetHitsoundVolume(savedHitsoundVolume);
        }
        else
        {
            if (!musicMuted)
                savedMusicVolume = GetMusicVolume();
            if (!hitsoundMuted)
                savedHitsoundVolume = GetHitsoundVolume();
            SetMusicVolume(0f);
            SetHitsoundVolume(0f);
        }

        musicMuted = hitsoundMuted = muted = !muted;

        UpdateSprites();
    }


    public void OnMuteMusicButtonClick()
    {
        if(musicMuted)
        {
            SetMusicVolume(savedMusicVolume);
            muted = false;
        }
        else
        {
            savedMusicVolume = GetMusicVolume();
            SetMusicVolume(0f);
        }

        musicMuted = !musicMuted;

        if(musicMuted && hitsoundMuted)
            muted = true;

        UpdateSprites();
    }


    public void OnMuteHitSoundsButtonClick()
    {
        if(hitsoundMuted)
        {
            SetHitsoundVolume(savedHitsoundVolume);
            muted = false;
        }
        else
        {
            savedHitsoundVolume = GetHitsoundVolume();
            SetHitsoundVolume(0f);
        }

        hitsoundMuted = !hitsoundMuted;

        if(musicMuted && hitsoundMuted)
            muted = true;

        UpdateSprites();
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        ShowSliders();
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        HideSliders();
    }


    public void OnMusicTextSelected()
    {
        musicTextSelected = true;
        ShowSliders();
    }


    public void OnMusicTextDeselected()
    {
        musicTextSelected = false;
        HideSliders();
    }


    public void OnHitsoundTextSelected()
    {
        hitsoundTextSelected = true;
        ShowSliders();
    }


    public void OnHitsoundTextDeselected()
    {
        hitsoundTextSelected = false;
        HideSliders();
    }


    public void SetMusicVolumeSlider(float value)
    {
        SetMusicVolume(value * sliderStepScale);
        UpdateValueDisplay();
    }


    public void SetHitsoundVolumeSlider(float value)
    {
        SetHitsoundVolume(value * sliderStepScale);
        UpdateValueDisplay();
    }


    public void SetMusicVolume(string value)
    {
        if(float.TryParse(value, out float newVolume))
        {
            SetMusicVolume(newVolume);
        }

        //Force deselect the input field
        EventSystemHelper.SetSelectedGameObject(null);
        UpdateValueDisplay();
    }


    public void SetHitsoundVolume(string value)
    {
        if(float.TryParse(value, out float newVolume))
        {
            SetHitsoundVolume(newVolume);
        }

        //Force deselect the input field
        EventSystemHelper.SetSelectedGameObject(null);
        UpdateValueDisplay();
    }


    private void SetMusicVolume(float musicVolume)
    {
        musicVolume = (float)Math.Round(musicVolume, 2);
        SettingsManager.SetRule(musicRule, musicVolume);
    }


    private void SetHitsoundVolume(float hitsoundVolume)
    {
        hitsoundVolume = (float)Math.Round(hitsoundVolume, 2);
        SettingsManager.SetRule(hitsoundRule, hitsoundVolume);
    }


    private float GetMusicVolume()
    {
        float musicVolume = Mathf.Clamp01(SettingsManager.GetFloat(musicRule));
        return (float)Math.Round(musicVolume, 2);
    }


    private float GetHitsoundVolume()
    {
        float hitsoundVolume = Mathf.Clamp01(SettingsManager.GetFloat(hitsoundRule));
        return (float)Math.Round(hitsoundVolume, 2);
    }


    private void UpdateSprites()
    {
        mainImage.sprite = muted ? mainMutedSprite : mainActiveSprite;
        musicImage.sprite = musicMuted ? musicMutedSprite : musicActiveSprite;
        hitsoundImage.sprite = hitsoundMuted ? hitsoundMutedSprite : hitsoundActiveSprite;
    }


    private void UpdateValueDisplay()
    {
        UpdateSprites();

        float musicVolume = GetMusicVolume();
        float hitsoundVolume = GetHitsoundVolume();

        musicSlider.SetValueWithoutNotify(musicVolume / sliderStepScale);
        musicInputField.SetTextWithoutNotify(musicVolume <= 0 ? "Off" : musicVolume.ToString());

        hitsoundSlider.SetValueWithoutNotify(hitsoundVolume / sliderStepScale);
        hitsoundInputField.SetTextWithoutNotify(hitsoundVolume <= 0 ? "Off" : hitsoundVolume.ToString());
    }


    private void UpdateSettings(string setting)
    {
        if(!sliderContainer.activeInHierarchy)
        {
            return;
        }

        if(setting == "all" || setting == musicRule || setting == hitsoundRule)
        {
            UpdateValueDisplay();
        }
    }


    private void Update()
    {
        if(hovered && Input.GetMouseButtonDown(0))
        {
            //Weird hack since IPointerDownHandler doesn't include children in raycast for some reason
            clicked = true;
        }
        else if(clicked && !Input.GetMouseButton(0))
        {
            clicked = false;
            HideSliders();
        }
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;
        HideSliders();
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
    }
}