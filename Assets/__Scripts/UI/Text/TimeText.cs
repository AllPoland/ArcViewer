using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TimeText : MonoBehaviour
{
    private TextMeshProUGUI text;


    public void UpdateText(float beat)
    {
        string timeText = Math.Round(TimeManager.CurrentTime, 2).ToString();
        string beatText = ((int)beat).ToString();

        text.text = $"{timeText}<br>{beatText}";
    }


    private void OnEnable()
    {
        text = GetComponent<TextMeshProUGUI>();
        if(text == null)
        {
            Debug.LogWarning("There needs to be a text component on the gameobject!");
            this.enabled = false;
            return;
        }

        TimeManager.OnBeatChanged += UpdateText;
    }


    private void OnDisable()
    {
        TimeManager.OnBeatChanged -= UpdateText;
    }
}