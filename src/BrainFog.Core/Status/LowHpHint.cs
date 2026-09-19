namespace BrainFog.Core.Status;

/// <summary>
/// Low-HP warning (spec 0.02 #3): true HP below 15% of max HP shows a warning,
/// even though the displayed HP is a stale snapshot.
/// </summary>
public static class LowHpHint
{
    public const int ThresholdPercent = 15;

    public const string Message = "濒危：我感觉自己快死了";

    public static bool IsLow(int trueHp, int maxHp) =>
        maxHp > 0 && trueHp > 0 && trueHp * 100 < maxHp * ThresholdPercent;
}
