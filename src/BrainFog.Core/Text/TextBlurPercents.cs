namespace BrainFog.Core.Text;

/// <summary>
/// Central blur ratios (user rules 2026-09-19). Any text that is garbled but has
/// no explicit rule uses <see cref="Default"/>.
/// </summary>
public static class TextBlurPercents
{
    /// <summary>Fallback for otherwise unspecified garbled text.</summary>
    public const int Default = 60;

    /// <summary>Potion name/description.</summary>
    public const int Potion = 50;

    /// <summary>Keyword descriptions in card-view / upgrade screens.</summary>
    public const int CardViewTips = 50;

    /// <summary>Map/event story text.</summary>
    public const int Event = 75;

    /// <summary>Title screen and pause menu options.</summary>
    public const int Menu = 75;

    /// <summary>Top bar / map UI descriptions (HP, gold, floor, map, deck, share, legend).</summary>
    public const int UiDescription = 70;

    /// <summary>Revealed card face text.</summary>
    public const int CardFaceText = 85;

    /// <summary>Ancient / Architect / merchant dialogue.</summary>
    public const int Dialogue = 90;
}
