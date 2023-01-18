using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayButton : MonoBehaviour
{
    [SerializeField] private Sprite PlaySprite;
    [SerializeField] private Sprite PauseSprite;

    private TimeManager timeManager;
    private Image image;


    public void TogglePlaying()
    {
        timeManager.TogglePlaying();
    }


    public void UpdatePlaying(bool newPlaying)
    {
        image.sprite = !newPlaying && !timeManager.ForcePause ? PlaySprite : PauseSprite;
    }


    private void Update()
    {
        if(Input.GetButtonDown("Pause"))
        {
            TogglePlaying();
        }
    }


    private void Start()
    {
        timeManager = TimeManager.Instance;

        if(timeManager != null)
        {
            timeManager.OnPlayingChanged += UpdatePlaying;
        }

        image = GetComponent<Image>();
    }
}