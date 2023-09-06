using System;

[Serializable]
public class HsvConfig
{
    public string displayMode;
    public int timeDependencyDecimalPrecision;
    public int timeDependencyDecimalOffset;

    public HsvJudgement[] judgments;
    public HsvJudgementSegment[] beforeCutAngleJudgments;
    public HsvJudgementSegment[] accuracyJudgments;
    public HsvJudgementSegment[] afterCutAngleJudgments;
    public HsvTimeDependencyJudgement[] timeDependencyJudgments;
}


[Serializable]
public class HsvJudgement
{
    public int threshold;
    public string text;
    public float[] color;
    public bool fade;
}


[Serializable]
public class HsvJudgementSegment
{
    public int threshold;
    public string text;
}


[Serializable]
public class HsvTimeDependencyJudgement
{
    public float threshold;
    public string text;
}