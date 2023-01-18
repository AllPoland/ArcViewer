using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TimeText : MonoBehaviour
{
    private TextMeshProUGUI text;
    private TimeManager timeManager;


    public void UpdateText(float beat)
    {
        string timeText = Math.Round(timeManager.CurrentTime, 2).ToString();
        string beatText = ((int)beat).ToString();

        text.text = $"{timeText}<br>{beatText}";
    }


    private void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        if(text == null)
        {
            Debug.Log("There needs to be a text component on the gameobject.");
            Destroy(gameObject);
            return;
        }

        timeManager = TimeManager.Instance;
        if(timeManager != null)
        {
            timeManager.OnBeatChanged += UpdateText;
        }
        else
        {
            Debug.Log("Found no Time Manager.");
            Destroy(gameObject);
            return;
        }
    }
}