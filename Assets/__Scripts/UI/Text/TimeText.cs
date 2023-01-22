using System;
using UnityEngine;
using TMPro;

public class TimeText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI beatText;


    public void UpdateText(float beat)
    {
        uint currentTime = (uint)TimeManager.CurrentTime;
        uint currentSeconds = currentTime % 60;
        uint currentMinutes = currentTime / 60;

        string secondsString = currentSeconds >= 10 ? $"{currentSeconds}" : $"0{currentSeconds}";

        timeText.text = $"{currentMinutes}:{secondsString}";
        beatText.text = ((uint)beat).ToString();
    }


    private void OnEnable()
    {
        TimeManager.OnBeatChanged += UpdateText;
    }


    private void OnDisable()
    {
        TimeManager.OnBeatChanged -= UpdateText;
    }
}