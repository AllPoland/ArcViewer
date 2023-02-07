using UnityEngine;

public class SkipButton : MonoBehaviour
{
    [SerializeField] private float skipAmount = 5f;
    [SerializeField] private float shortSkipBeats = 1f;
    

    public void Skip(float amount)
    {
        if(TimeManager.ForcePause)
        {
            return;
        }
        bool wasPlaying = TimeManager.Playing;

        TimeManager.SetPlaying(false);
        TimeManager.CurrentTime += amount;
        TimeManager.SetPlaying(wasPlaying);
    }


    private void Update()
    {
        if(Input.GetButtonDown("SkipForward"))
        {
            Skip(skipAmount);
        }
        else if(Input.GetButtonDown("SkipBackward"))
        {
            Skip(skipAmount * -1f);
        }
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if(!TimeManager.Playing && Mathf.Abs(scroll) > 0)
        {
            float skipTime = TimeManager.RawTimeFromBeat(shortSkipBeats, TimeManager.CurrentBPM);
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