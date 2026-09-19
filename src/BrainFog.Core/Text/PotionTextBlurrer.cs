namespace BrainFog.Core.Text;

/// <summary>
/// Deterministic 50% blur for potion name/description (user change 2026-09-19):
/// the same text always produces the same result ("固定不变").
/// </summary>
public static class PotionTextBlurrer
{
    public const int BlurPercent = TextBlurPercents.Potion;

    public static string Blur(string? text) => EventTextBlurrer.Blur(text, BlurPercent);
}
