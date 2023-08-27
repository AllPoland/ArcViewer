using UnityEngine;

public class SkipButton : MonoBehaviour
{
    [SerializeField] private float skipAmount = 5f;
    [SerializeField] private float shortSkipBeats = 1f;

    private float scaledSkipAmount => skipAmount * TimeSyncHandler.TimeScale;
    private float scaledShortSkipBeats => shortSkipBeats * TimeSyncHandler.TimeScale;


    public void Skip(float amount)
    {
        if(TimeManager.Scrubbing)
        {
            return;
        }
        bool wasPlaying = TimeManager.Playing;

        TimeManager.SetPlaying(false);
        TimeManager.CurrentTime += amount;
        TimeManager.SetPlaying(wasPlaying && TimeManager.CurrentTime < SongManager.GetSongLength());
    }


    private void Update()
    {
        if(DialogueHandler.LogActive)
        {
            return;
        }

        if(!EventSystemHelper.SelectedObject)
        {
            if(Input.GetButtonDown("SkipForward"))
            {
                Skip(scaledSkipAmount);
            }
            else if(Input.GetButtonDown("SkipBackward"))
            {
                Skip(-scaledSkipAmount);
            }
            else if(Input.GetButtonDown("SkipForwardDouble"))
            {
                Skip(2 * scaledSkipAmount);
            }
            else if(Input.GetButtonDown("SkipBackwardDouble"))
            {
                Skip(-2 * scaledSkipAmount);
            }
        }

        if(!TimeManager.Playing && (!SettingsMenu.Open || !UserIdleDetector.MouseOnUI))
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if(Mathf.Abs(scroll) > 0)
            {
                float skipTime = TimeManager.RawTimeFromBeat(scaledShortSkipBeats, TimeManager.CurrentBPM);
                if(scroll > 0)
                {
                    Skip(skipTime);
                }
                else
                {
                    Skip(-skipTime);
                }
            }
        }
    }
}