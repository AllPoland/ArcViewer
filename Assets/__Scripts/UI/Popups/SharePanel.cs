using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SharePanel : MonoBehaviour
{
    public bool UseTimestamp;

    [SerializeField] private TMP_InputField urlOutput;
    [SerializeField] private Toggle timeStampToggle;
    [SerializeField] private TextMeshProUGUI timeStampToggleLabel;


    public void SetEnableTimestamp(bool timestamp)
    {
        UseTimestamp = timestamp;
    }


    private void UpdateText()
    {
        string newText = UrlArgHandler.ArcViewerURL;

        if(!string.IsNullOrEmpty(UrlArgHandler.LoadedMapID))
        {
            newText += $"?id={UrlArgHandler.LoadedMapID}";
        }
        else if(!string.IsNullOrEmpty(UrlArgHandler.LoadedMapURL))
        {
            newText += $"?url={UrlArgHandler.LoadedMapURL}";
        }
        else
        {
            //The map was loaded locally, this menu shouldn't be open
            gameObject.SetActive(false);
            return;
        }

        if(UseTimestamp)
        {
            float time = (ulong)TimeManager.CurrentTime;
            newText += $"&t={time}";
        }

        string mode = UrlArgHandler.LoadedCharacteristic?.ToString() ?? "";
        string difficulty = UrlArgHandler.LoadedDiffRank?.ToString() ?? "";

        if(!string.IsNullOrEmpty(mode))
        {
            newText += $"&mode={mode}";
        }
        if(!string.IsNullOrEmpty(difficulty))
        {
            newText += $"&difficulty={difficulty}";
        }

        if(newText != urlOutput.text)
        {
            urlOutput.text = newText;
        }
    }


    private void UpdateToggleLabel()
    {
        ulong currentTime = (ulong)TimeManager.CurrentTime;
        ulong currentSeconds = currentTime % 60;
        ulong currentMinutes = currentTime / 60;

        string secondsString = currentSeconds >= 10 ? $"{currentSeconds}" : $"0{currentSeconds}";

        string time = $"{currentMinutes}:{secondsString}";

        timeStampToggleLabel.text = $"Start at {time}";
    }


    private void Update()
    {
        UpdateText();
        UpdateToggleLabel();
    }


    private void OnEnable()
    {
        timeStampToggle.SetIsOnWithoutNotify(UseTimestamp);
    }
}