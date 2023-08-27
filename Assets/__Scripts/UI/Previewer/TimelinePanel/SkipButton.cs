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

    public void SkipFrame(bool isForward)
    {
        // If replay is not found default to skipping 1/60 seconds
        if(!ReplayManager.IsReplayMode)
        {
            var mult = isForward ? 1 : -1;
            Skip(mult * TimeSyncHandler.TimeScale / 60f);
            return;
        }

        var frameToSkipTo = isForward
            ? ReplayManager.CurrentReplay.frames.Find(frame => frame.time > TimeManager.CurrentTime + 0.001f)
            : ReplayManager.CurrentReplay.frames.FindLast(frame => frame.time < TimeManager.CurrentTime - 0.001f);
        if(frameToSkipTo != null)
        {
            Skip(frameToSkipTo.time - TimeManager.CurrentTime);
        }
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

            if(!TimeManager.Playing)
            {
                if(Input.GetButtonDown("SkipForwardFrame"))
                {
                    SkipFrame(isForward: true);
                }
                else if(Input.GetButtonDown("SkipBackwardFrame"))
                {
                    SkipFrame(isForward: false);
                }
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