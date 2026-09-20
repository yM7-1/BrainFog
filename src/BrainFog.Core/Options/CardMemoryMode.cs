namespace BrainFog.Core.Options;

/// <summary>
/// Card memory mode (user change 2026-09-21, replaces the old same-name/reveal-all
/// toggles):
/// - Omniscient ("通晓万物"): every card face is globally revealed;
/// - GoodMemory ("好记性"): playing/upgrading one copy reveals the whole
///   definition for the run, and known cards show their face everywhere,
///   including acquisition screens (overrides the reward/shop/event settings);
/// - BadMemory ("坏记性"): only the played copy is revealed; a revealed copy
///   drawn into hand <c>n</c> times without being played reverts to unknown;
/// - Nonsense ("歪比巴卜"): cards are never revealed (overrides acquisition
///   reveal settings).
/// </summary>
public enum CardMemoryMode
{
    Omniscient = 0,
    GoodMemory = 1,
    BadMemory = 2,
    Nonsense = 3,
}

public static class CardMemoryModeRules
{
    public static string ToStorage(CardMemoryMode mode) => mode switch
    {
        CardMemoryMode.Omniscient => "omniscient",
        CardMemoryMode.BadMemory => "bad",
        CardMemoryMode.Nonsense => "nonsense",
        _ => "good",
    };

    public static CardMemoryMode Parse(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "omniscient" => CardMemoryMode.Omniscient,
            "bad" => CardMemoryMode.BadMemory,
            "nonsense" => CardMemoryMode.Nonsense,
            _ => CardMemoryMode.GoodMemory,
        };

    public static int ToIndex(CardMemoryMode mode) => mode switch
    {
        CardMemoryMode.Omniscient => 0,
        CardMemoryMode.BadMemory => 2,
        CardMemoryMode.Nonsense => 3,
        _ => 1,
    };

    public static CardMemoryMode FromIndex(int index) => index switch
    {
        0 => CardMemoryMode.Omniscient,
        2 => CardMemoryMode.BadMemory,
        3 => CardMemoryMode.Nonsense,
        _ => CardMemoryMode.GoodMemory,
    };
}
