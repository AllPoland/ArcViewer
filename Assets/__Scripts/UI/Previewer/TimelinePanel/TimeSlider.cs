using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TimeSlider : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Slider slider;
    [SerializeField] private GameObject timeTooltip;

    private bool clicking;
    private bool mouseOver;


    public void UpdateSlider(float beat)
    {
        slider.SetValueWithoutNotify(TimeManager.Progress);
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if(clicking || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        clicking = true;

        //Force the song to pause if it's playing
        TimeManager.Scrubbing = true;
        TimeManager.ForcePause = TimeManager.Playing;

        TimeManager.SetPlaying(false);
        TimeManager.Progress = slider.value;

        timeTooltip.SetActive(true);
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        if(!clicking || Input.GetMouseButton(0))
        {
            return;
        }

        TimeManager.Scrubbing = false;
        if(TimeManager.ForcePause && TimeManager.Progress < 1)
        {
            TimeManager.ForcePause = false;
            TimeManager.SetPlaying(true);
        }
        else
        {
            //Automatically pause if setting to the end of the song
            TimeManager.ForcePause = false;
            TimeManager.SetPlaying(false);
        }
        clicking = false;

        timeTooltip.SetActive(mouseOver);
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseOver = true;
        timeTooltip.SetActive(true);
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        mouseOver = false;
        timeTooltip.SetActive(clicking);
    }


    private void Update()
    {
        if(clicking)
        {
            TimeManager.Progress = slider.value;
        }
    }


    private void OnEnable()
    {
        TimeManager.OnBeatChanged += UpdateSlider;

        slider = GetComponent<Slider>();
    }
}