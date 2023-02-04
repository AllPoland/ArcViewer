using UnityEngine;
using TMPro;

public class TimeText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI beatText;


    public void UpdateText(float beat)
    {
        ulong currentTime = (ulong)TimeManager.CurrentTime;
        ulong currentSeconds = currentTime % 60;
        ulong currentMinutes = currentTime / 60;

        string secondsString = currentSeconds >= 10 ? $"{currentSeconds}" : $"0{currentSeconds}";

        timeText.text = $"{currentMinutes}:{secondsString}";
        beatText.text = ((ulong)beat).ToString();
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