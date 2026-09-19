namespace BrainFog.Core.Options;

/// <summary>How many of the offered cards in a card-reward selection screen are
/// revealed (the revealed slots are picked at random, stable per offer).</summary>
public enum SelectionRevealOption
{
    None = 0,
    RandomOne = 1,
    RandomTwo = 2,
    RandomThree = 3,
    All = 4,
}

/// <summary>
/// Player-facing cognition-modifier options (in-game panel).
/// Pure model: storage encoding and index rules are testable without Godot.
/// </summary>
public sealed class DifficultySettings
{
    /// <summary>Reveal the first N offered cards in card-reward selection screens.</summary>
    public SelectionRevealOption SelectionReveal { get; set; } = SelectionRevealOption.None;

    /// <summary>Reveal card faces in shop stock and event acquisition screens.</summary>
    public bool RevealShopAndEventCards { get; set; }

    /// <summary>Playing/upgrading one copy reveals every copy of that card for the run.</summary>
    public bool RevealSameNameCards { get; set; } = true;

    /// <summary>Show live HP and gold instead of the stale snapshot.</summary>
    public bool ShowLiveStatus { get; set; }

    /// <summary>Show relics the player already owns (inventory/inspect).</summary>
    public bool ShowOwnedRelics { get; set; }

    /// <summary>Show every map node and route instead of the fogged frontier.</summary>
    public bool ShowAllMapRoutes { get; set; }

    /// <summary>Enemy intents stay visible every turn instead of only the first round.</summary>
    public bool ShowEnemyIntents { get; set; }

    public static string ToStorage(SelectionRevealOption option) => option switch
    {
        SelectionRevealOption.RandomOne => "1",
        SelectionRevealOption.RandomTwo => "2",
        SelectionRevealOption.RandomThree => "3",
        SelectionRevealOption.All => "all",
        _ => "none",
    };

    public static SelectionRevealOption ParseSelectionReveal(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "1" => SelectionRevealOption.RandomOne,
            "2" => SelectionRevealOption.RandomTwo,
            "3" => SelectionRevealOption.RandomThree,
            "all" => SelectionRevealOption.All,
            _ => SelectionRevealOption.None,
        };

    public static int ToIndex(SelectionRevealOption option) => option switch
    {
        SelectionRevealOption.RandomOne => 1,
        SelectionRevealOption.RandomTwo => 2,
        SelectionRevealOption.RandomThree => 3,
        SelectionRevealOption.All => 4,
        _ => 0,
    };

    public static SelectionRevealOption FromIndex(int index) => index switch
    {
        1 => SelectionRevealOption.RandomOne,
        2 => SelectionRevealOption.RandomTwo,
        3 => SelectionRevealOption.RandomThree,
        4 => SelectionRevealOption.All,
        _ => SelectionRevealOption.None,
    };
}
