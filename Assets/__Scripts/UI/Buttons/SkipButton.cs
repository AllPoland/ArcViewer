using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkipButton : MonoBehaviour
{
    [SerializeField] private float skipAmount = 5f;

    

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
    }
}