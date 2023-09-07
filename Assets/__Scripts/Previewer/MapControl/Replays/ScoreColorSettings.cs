using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable]
public class ScoreColorSettings
{
    [Header("Configuration")]
    [SerializeField] public FormatMode formatMode = FormatMode.Numeric;
    [SerializeField] public int timeDependencyDecimals;
    [SerializeField] public int timeDependencyMult;

    [Header("Judgements")]
    [SerializeField] public List<ScoreJudgement> scoreJudgements = new List<ScoreJudgement> { new ScoreJudgement() };
    [SerializeField] public List<HsvJudgementSegment> preSwingJudgements = new List<HsvJudgementSegment>();
    [SerializeField] public List<HsvJudgementSegment> accJudgements = new List<HsvJudgementSegment>();
    [SerializeField] public List<HsvJudgementSegment> postSwingJudgements = new List<HsvJudgementSegment>();
    [SerializeField] public List<HsvTimeDependencyJudgement> timeDependencyJudgements = new List<HsvTimeDependencyJudgement>();
    [SerializeField] public Color chainLinkColor = Color.white;


    private string GetFormattedScoreText(ScoreJudgement judgement, ScoringEvent scoringEvent)
    {
        float timeDependency = (float)Math.Round(scoringEvent.TimeDependency * timeDependencyMult, timeDependencyDecimals);

        HsvJudgementSegment preSwingJudgement = preSwingJudgements.FirstOrDefault(x => x.threshold <= scoringEvent.PreSwingScore);
        HsvJudgementSegment accJudgement = accJudgements.FirstOrDefault(x => x.threshold <= scoringEvent.AccuracyScore);
        HsvJudgementSegment postSwingJudgement = postSwingJudgements.FirstOrDefault(x => x.threshold <= scoringEvent.PostSwingScore);

        HsvTimeDependencyJudgement timeDependencyJudgement = timeDependencyJudgements.FirstOrDefault(x => x.threshold <= scoringEvent.TimeDependency);

        var builder = new StringBuilder();
        var formatString = judgement.text;
        var nextPercentIndex = formatString.IndexOf('%');
        while (nextPercentIndex != -1)
        {
            builder.Append(formatString.Substring(0, nextPercentIndex));
            if (formatString.Length == nextPercentIndex + 1)
            {
                formatString += " ";
            }

            var specifier = formatString[nextPercentIndex + 1];

            switch (specifier)
            {
                case 'b':
                    builder.Append(scoringEvent.PreSwingScore);
                    break;
                case 'c':
                    builder.Append(scoringEvent.AccuracyScore);
                    break;
                case 'a':
                    builder.Append(scoringEvent.PostSwingScore);
                    break;
                case 't':
                    builder.Append(timeDependency);
                    break;
                case 'B':
                    builder.Append(preSwingJudgement.text ?? "");
                    break;
                case 'C':
                    builder.Append(accJudgement.text ?? "");
                    break;
                case 'A':
                    builder.Append(postSwingJudgement.text ?? "");
                    break;
                case 'T':
                    builder.Append(timeDependencyJudgement.text ?? "");
                    break;
                case 's':
                    builder.Append(scoringEvent.ScoreGained);
                    break;
                case 'p':
                    builder.Append($"{(double)scoringEvent.ScoreGained / scoringEvent.MaxSwingScore * 100:0}");
                    break;
                case '%':
                    builder.Append("%");
                    break;
                case 'n':
                    builder.Append("<br>");
                    break;
                default:
                    builder.Append("%" + specifier);
                    break;
            }

            formatString = formatString.Remove(0, nextPercentIndex + 2);
            nextPercentIndex = formatString.IndexOf('%');
        }

        return builder.Append(formatString).ToString();
    }


    private string GetScoreText(ScoreJudgement judgement, ScoringEvent scoringEvent)
    {
        switch(formatMode)
        {
            case FormatMode.Format:
                return GetFormattedScoreText(judgement, scoringEvent);
            case FormatMode.Numeric:
                return scoringEvent.ScoreGained.ToString();
            case FormatMode.TextOnly:
                return judgement.text;
            case FormatMode.ScoreOnTop:
            default:
                return $"{scoringEvent.ScoreGained.ToString()}<br>{judgement.text}<br>";
        }
    }


    private ScoreTextInfo GetTextInfoFromScore(ScoreJudgement judgement, ScoreJudgement lastJudgement, ScoringEvent scoringEvent)
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

        newInfo.color.a = Mathf.Clamp(newInfo.color.a, 0f, 1f);
        newInfo.text = GetScoreText(judgement, scoringEvent);

        return newInfo;
    }


    public ScoreTextInfo GetScoreTextInfo(ScoringEvent scoringEvent)
    {
        int scoreGained = scoringEvent.ScoreGained;
        if(scoringEvent.scoringType == ScoringType.ChainHead)
        {
            scoreGained += ScoreManager.PostSwingValue;
        }

        for(int i = 0; i < scoreJudgements.Count; i++)
        {
            ScoreJudgement judgement = scoreJudgements[i];

            if(judgement.scoreThreshold <= scoreGained)
            {
                ScoreJudgement lastJudgement = i > 0 ? scoreJudgements[i - 1] : null;
                return GetTextInfoFromScore(judgement, lastJudgement, scoringEvent);
            }
        }

        return new ScoreTextInfo(scoringEvent.ScoreGained);
    }


    public ScoreColorSettings()
    {
        formatMode = FormatMode.Numeric;
    }
    
    public ScoreColorSettings(HsvConfig config)
    {
        switch(config.displayMode)
        {
            case "format":
                formatMode = FormatMode.Format;
                break;
            case "numeric":
                formatMode = FormatMode.Numeric;
                break;
            case "textOnly":
                formatMode = FormatMode.TextOnly;
                break;
            case "scoreOnTop":
            default:
                formatMode = FormatMode.ScoreOnTop;
                break;
        }

        timeDependencyDecimals = Mathf.Clamp(config.timeDependencyDecimalPrecision, 0, 99);
        timeDependencyMult = (int)Mathf.Pow(10, Mathf.Clamp(config.timeDependencyDecimalOffset, 0, 38));

        if(config.judgments != null && config.judgments.Length > 0)
        {
            scoreJudgements = new List<ScoreJudgement>();
            foreach(HsvJudgement judgement in config.judgments)
            {
                scoreJudgements.Add(new ScoreJudgement(judgement));
            }
            scoreJudgements = scoreJudgements.OrderByDescending(x => x.scoreThreshold).ToList();
        }

        if(config.beforeCutAngleJudgments != null)
        {
            preSwingJudgements.AddRange(config.beforeCutAngleJudgments.OrderByDescending(x => x.threshold));
        }
        if(config.accuracyJudgments != null)
        {
            accJudgements.AddRange(config.accuracyJudgments.OrderByDescending(x => x.threshold));
        }
        if(config.afterCutAngleJudgments != null)
        {
            postSwingJudgements.AddRange(config.afterCutAngleJudgments.OrderByDescending(x => x.threshold));
        }
        if(config.timeDependencyJudgments != null)
        {
            timeDependencyJudgements.AddRange(config.timeDependencyJudgments.OrderByDescending(x => x.threshold));
        }
    }


    public enum FormatMode
    {
        Format,
        Numeric,
        TextOnly,
        ScoreOnTop
    }
}


[Serializable]
public class ScoreJudgement
{
    public Color color = Color.white;
    public int scoreThreshold = 0;
    public string text = "%s";
    public bool fade;


    public ScoreJudgement() { }

    public ScoreJudgement(HsvJudgement hsvJudgement)
    {
        scoreThreshold = hsvJudgement.threshold;
        text = hsvJudgement.text;
        fade = hsvJudgement.fade;

        float[] hsvColor = hsvJudgement.color;
        if(hsvColor != null && hsvColor.Length >= 4)
        {
            color = new Color(hsvColor[0], hsvColor[1], hsvColor[2], hsvColor[3]);
        }
        else color = Color.white;
    }
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

    public ScoreTextInfo(int score, Color scoreColor)
    {
        text = score.ToString();
        color = scoreColor;
    }
}