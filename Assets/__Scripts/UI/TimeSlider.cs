using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TimeSlider : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Slider slider;

    private TimeManager timeManager;
    private bool Playing;
    private bool clicking;
    private bool clickPaused;


    public void UpdateSlider(float beat)
    {
        slider.value = timeManager.Progress;
    }


    public void SetProgress(float value)
    {
        timeManager.Progress = value;
    }


    public void UpdatePlaying(bool newPlaying)
    {
        Playing = newPlaying;
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        clicking = true;
        clickPaused = timeManager.Playing;

        timeManager.ForcePause = true;
        timeManager.SetPlaying(false);
        SetProgress(slider.value);
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        timeManager.ForcePause = false;
        
        if(clickPaused)
        {
            timeManager.SetPlaying(true);
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
        timeManager = TimeManager.Instance;

        if(timeManager != null)
        {
            timeManager.OnBeatChanged += UpdateSlider;
            timeManager.OnPlayingChanged += UpdatePlaying;
        }

        slider = GetComponent<Slider>();
    }
}