using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TimeSlider : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Slider slider;

    private bool Playing;
    private bool clicking;
    private bool clickPaused;


    public void UpdateSlider(float beat)
    {
        slider.value = TimeManager.Progress;
    }


    public void SetProgress(float value)
    {
        TimeManager.Progress = value;
    }


    public void UpdatePlaying(bool newPlaying)
    {
        Playing = newPlaying;
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        clicking = true;
        clickPaused = TimeManager.Playing;

        TimeManager.ForcePause = true;
        TimeManager.SetPlaying(false);
        SetProgress(slider.value);
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        TimeManager.ForcePause = false;
        
        if(clickPaused)
        {
            TimeManager.SetPlaying(true);
        }
        clickPaused = false;
        clicking = false;
    }


    private void Update()
    {
        if(clicking)
        {
            SetProgress(slider.value);
        }
    }


    private void Start()
    {
        TimeManager.OnBeatChanged += UpdateSlider;
        TimeManager.OnPlayingChanged += UpdatePlaying;

        slider = GetComponent<Slider>();
    }
}