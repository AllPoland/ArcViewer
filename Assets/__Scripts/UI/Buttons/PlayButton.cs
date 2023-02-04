using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayButton : MonoBehaviour
{
    [SerializeField] private Sprite PlaySprite;
    [SerializeField] private Sprite PauseSprite;

    private Image image;


    public void TogglePlaying()
    {
        TimeManager.TogglePlaying();
    }


    public void UpdatePlaying(bool newPlaying)
    {
        image.sprite = newPlaying || TimeManager.ForcePause ? PauseSprite : PlaySprite;
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
        TimeManager.OnPlayingChanged += UpdatePlaying;

        image = GetComponent<Image>();
    }
}