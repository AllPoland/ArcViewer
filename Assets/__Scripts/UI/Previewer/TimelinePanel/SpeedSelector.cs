using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpeedSelector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color highlightedColor;

    [SerializeField] private GameObject sliderContainer;
    [SerializeField] private Slider slider;

    private Image image;
    private bool hovered;
    private bool clicked;


    public void ShowSlider()
    {
        image.color = highlightedColor;

        sliderContainer.SetActive(true);
        UpdateSliderValue();
    }


    public void HideSlider()
    {
        //Don't hide the slider if the mouse is over us, or if we're still clicked on
        if(hovered || clicked) return;

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


    public void SetSpeed(float value)
    {
        //Multiply by 0.1 so that the slider can move in steps of 0.1
        TimeSyncHandler.TimeScale = (int)value * 0.1f;
    }


    private void UpdateSliderValue()
    {
        slider.SetValueWithoutNotify((TimeSyncHandler.TimeScale * 10));
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
        
        if(Input.GetButtonDown("IncreaseSpeed"))
        {
            float newScale = (float)Math.Round(TimeSyncHandler.TimeScale + 0.1f, 1);
            TimeSyncHandler.TimeScale = Mathf.Clamp(newScale, 0, 2);
            UpdateSliderValue();
        }
        else if(Input.GetButtonDown("DecreaseSpeed"))
        {
            float newScale = (float)Math.Round(TimeSyncHandler.TimeScale - 0.1f, 1);
            TimeSyncHandler.TimeScale = Mathf.Clamp(newScale, 0, 2);
            UpdateSliderValue();
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