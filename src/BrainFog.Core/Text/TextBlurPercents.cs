namespace BrainFog.Core.Text;

/// <summary>
/// Single blur ratio for every garbled text (user change 2026-09-21): the
/// player-facing slider in the cognition modifier controls it (0–100, 1% step).
/// Exemptions that stay readable regardless of the ratio: enemy intents, the
/// settings screen, the card compendium and the mod's own panel.
/// </summary>
public static class TextBlurPercents
{
    /// <summary>Default ratio before the player moves the slider.</summary>
    public const int Default = 50;
}
