using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ScoreColorSettings
{
    [SerializeField] public List<ScoreColor> scoreColors = new List<ScoreColor> { new ScoreColor() };
    [SerializeField] public Color chainLinkColor = Color.white;


    public Color GetScoreColor(int score)
    {
        ScoreColor lastColor = scoreColors.FirstOrDefault(x => x.scoreThreshold <= score);
        return lastColor?.color ?? Color.white;
    }
}


[Serializable]
public class ScoreColor
{
    public Color color = Color.white;
    public int scoreThreshold = 0;
}