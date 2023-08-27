using UnityEngine;
using TMPro;

public class TimeText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI beatText;


    public void UpdateText(float beat)
    {
        int totalSeconds = Mathf.FloorToInt(TimeManager.CurrentTime);
        int currentSeconds = totalSeconds % 60;
        int currentMinutes = totalSeconds / 60;

        string secondsString = currentSeconds >= 10 ? $"{currentSeconds}" : $"0{currentSeconds}";

        timeText.text = $"{currentMinutes}:{secondsString}";
        beatText.text = Mathf.FloorToInt(beat).ToString();
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