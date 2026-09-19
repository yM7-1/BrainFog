namespace BrainFog.Core.Text;

/// <summary>
/// Deterministic 50% blur for potion name/description (user change 2026-09-19,
/// per-launch salt 2026-09-20): the same text always produces the same result
/// within a launch and a different one on the next launch.
/// </summary>
public static class PotionTextBlurrer
{
    public const int BlurPercent = TextBlurPercents.Potion;

    public static string Blur(string? text) => EventTextBlurrer.Blur(text, BlurPercent, BlurSalt.Current);
}
