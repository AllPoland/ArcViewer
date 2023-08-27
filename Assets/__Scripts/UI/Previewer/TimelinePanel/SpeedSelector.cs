using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class SpeedSelector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject sliderContainer;
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_InputField inputField;

    [SerializeField] private Color defaultColor;
    [SerializeField] private Color highlightedColor;
    [SerializeField] private string textSuffix;

    private Image image;
    private bool hovered;
    private bool clicked;
    private bool textSelected;

    private const float sliderStepScale = 0.05f;
    private const float bindingStepScale = 0.1f;


    public void ShowSlider()
    {
        image.color = highlightedColor;

        sliderContainer.SetActive(true);
        UpdateValueDisplay();
    }


    public void HideSlider()
    {
        //Don't hide the slider if the mouse is over us, or if we're still clicked on
        if(hovered || clicked || textSelected) return;

        image.color = defaultColor;

        sliderContainer.SetActive(false);
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        ShowSlider();
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        HideSlider();
    }


    public void OnTextSelected()
    {
        textSelected = true;
        ShowSlider();
        inputField.SetTextWithoutNotify(TimeSyncHandler.TimeScale.ToString());
    }


    public void OnTextDeselected()
    {
        textSelected = false;
        HideSlider();
    }


    public void SetSpeedSlider(float value)
    {
        //Multiply by 0.05 because the slider uses integers representing steps of 0.05
        SetTimeScale(value * sliderStepScale);
        UpdateValueDisplay();
    }


    public void SetSpeedString(string value)
    {
        if(float.TryParse(value, out float newSpeed))
        {
            SetTimeScale(newSpeed);
        }

        //Force deselect the input field
        EventSystemHelper.SetSelectedGameObject(null);
        UpdateValueDisplay();
    }


    public void ResetSpeed()
    {
        SetTimeScale(ReplayManager.IsReplayMode ? ReplayManager.ReplayTimeScale : 1f);
        UpdateValueDisplay();
    }


    private void SetTimeScale(float timeScale)
    {
        timeScale = (float)Math.Round(timeScale, 2);
        TimeSyncHandler.TimeScale = Mathf.Clamp(timeScale, 0, 2);
    }


    private void UpdateValueDisplay()
    {
        slider.SetValueWithoutNotify(TimeSyncHandler.TimeScale / sliderStepScale);
        inputField.SetTextWithoutNotify(TimeSyncHandler.TimeScale.ToString() + textSuffix);
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
            HideSlider();
        }
        
        if(!EventSystemHelper.SelectedObject)
        {
            if(Input.GetButtonDown("IncreaseSpeed"))
            {
                SetTimeScale(TimeSyncHandler.TimeScale + bindingStepScale);
                UpdateValueDisplay();
            }
            else if(Input.GetButtonDown("DecreaseSpeed"))
            {
                SetTimeScale(TimeSyncHandler.TimeScale - bindingStepScale);
                UpdateValueDisplay();
            }
        }
    }


    private void OnEnable()
    {
        if(!image)
        {
            image = GetComponent<Image>();
        }

        HideSlider();
    }
}