public static class ScoringUtils
{
    public const int MaxNoteScore = 115;
    public const int MaxChainHeadScore = 85;
    public const int MaxChainLinkScore = 20;

    public const int PreSwingValue = 70;
    public const int PostSwingValue = 30;

    public static readonly byte[] ComboMultipliers = { 1, 2, 4, 8 };
    public static readonly byte[] HitsNeededForComboIncrease = { 2, 4, 8, 255 };

    public static void ManuallyAdvanceCombo(ref byte comboMult, ref byte comboProgress)
    {
        if(comboMult >= ComboMultipliers.Length - 1) return;
        comboProgress++;
        if(comboProgress < HitsNeededForComboIncrease[comboMult]) return;
        comboMult++;
        comboProgress = 0;
    }
}
