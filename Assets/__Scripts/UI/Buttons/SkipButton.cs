using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkipButton : MonoBehaviour
{
    [SerializeField] private float skipAmount = 5f;

    private TimeManager timeManager;
    

    public void Skip(float amount)
    {
        if(timeManager.ForcePause)
        {
            return;
        }
        bool wasPlaying = timeManager.Playing;

        timeManager.SetPlaying(false);
        timeManager.CurrentTime += amount;
        timeManager.SetPlaying(wasPlaying);
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


    private void Start()
    {
        timeManager = TimeManager.Instance;
    }
}