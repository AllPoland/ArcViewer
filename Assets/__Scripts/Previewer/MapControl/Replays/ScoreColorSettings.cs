using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ScoreColorSettings
{
    [SerializeField] public List<ScoreJudgement> scoreJudgements = new List<ScoreJudgement> { new ScoreJudgement() };
    [SerializeField] public Color chainLinkColor = Color.white;


    private ScoreTextInfo ScoreTextFromJudgement(ScoreJudgement judgement, ScoreJudgement lastJudgement, ScoringEvent scoringEvent)
    {
        ScoreTextInfo newInfo = new ScoreTextInfo();

        if(judgement.fade && lastJudgement != null)
        {
            int thresholdDifference = lastJudgement.scoreThreshold - judgement.scoreThreshold;
            int scoreDifference = lastJudgement.scoreThreshold - scoringEvent.ScoreGained;

            float fadeProgress = (float)scoreDifference / thresholdDifference;
            newInfo.color = Color.Lerp(lastJudgement.color, judgement.color, fadeProgress);
        }
        else newInfo.color = judgement.color;

        string scoreText = judgement.text.Replace("%s", scoringEvent.ScoreGained.ToString());
        scoreText = scoreText.Replace("%b", scoringEvent.PreSwingScore.ToString());
        scoreText = scoreText.Replace("%c", scoringEvent.AccuracyScore.ToString());
        scoreText = scoreText.Replace("%a", scoringEvent.PostSwingScore.ToString());
        scoreText = scoreText.Replace("%n", "<br>");

        return newInfo;
    }


    public ScoreTextInfo GetScoreText(ScoringEvent scoringEvent)
    {
        for(int i = 0; i < scoreJudgements.Count; i++)
        {
            ScoreJudgement judgement = scoreJudgements[i];

            if(judgement.scoreThreshold <= scoringEvent.ScoreGained)
            {
                ScoreJudgement lastJudgement = i > 0 ? scoreJudgements[i - 1] : null;
                return ScoreTextFromJudgement(judgement, lastJudgement, scoringEvent);
            }
        }

        return new ScoreTextInfo(scoringEvent.ScoreGained);
    }
}


[Serializable]
public class ScoreJudgement
{
    public Color color = Color.white;
    public int scoreThreshold = 0;
    public string text = "%s";
    public bool fade;
}


public class ScoreTextInfo
{
    public Color color = Color.white;
    public string text = "";

    
    public ScoreTextInfo() { }

    public ScoreTextInfo(int score)
    {
        text = score.ToString();
    }
}