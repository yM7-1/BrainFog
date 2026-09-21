using BrainFog.Core.Text;

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
    /// <summary>Unified blur ratio for every garbled text (0 = fully readable,
    /// 100 = fully garbled); adjusted with the panel slider.</summary>
    public int TextBlurPercent { get; set; } = TextBlurPercents.Default;

    /// <summary>Fixed garbling vs re-rolled on every launch (2026-09-21).</summary>
    public BlurSaltMode SaltMode { get; set; } = BlurSaltMode.PerLaunch;

    /// <summary>Card-reward reveal level (default: reveal every reward card,
    /// user change 2026-09-21).</summary>
    public SelectionRevealOption SelectionReveal { get; set; } = SelectionRevealOption.All;

    /// <summary>Reveal card faces in shop stock and event acquisition screens
    /// (default on, user change 2026-09-21).</summary>
    public bool RevealShopAndEventCards { get; set; } = true;

    /// <summary>Card memory mode (通晓万物 / 好记性 / 坏记性 / 歪比巴卜).</summary>
    public CardMemoryMode MemoryMode { get; set; } = CardMemoryMode.BadMemory;

    /// <summary>"Bad memory" threshold n (≥1): unplayed hand entries before a
    /// revealed copy reverts to unknown (default 2, user change 2026-09-21).</summary>
    public int BadMemoryThreshold { get; set; } = 2;

    /// <summary>"Memory fade" (user change 2026-09-21): cards still unrevealed
    /// at the end of a combat are removed from the deck. Applies to good/bad
    /// memory; nonsense mode is exempt. On by default.</summary>
    public bool MemoryFade { get; set; } = true;

    /// <summary>HP/gold show the stale "state at last rest" snapshot instead of
    /// live values (default: live values, garbled like all text).</summary>
    public bool SnapshotStatus { get; set; }

    /// <summary>Show HP/gold numbers without garbling (default on, user change
    /// 2026-09-21; the display mode itself is still governed by SnapshotStatus).</summary>
    public bool ReadableStatusNumbers { get; set; } = true;

    /// <summary>Show relics everywhere (user change 2026-09-21, was "show owned
    /// relics"): owned inventory/inspect, rewards, shops, chests, run history.
    /// Default on (user change 2026-09-21).</summary>
    public bool ShowRelics { get; set; } = true;

    /// <summary>Show every map node and route instead of the fogged frontier
    /// (default on, user change 2026-09-21).</summary>
    public bool ShowAllMapRoutes { get; set; } = true;

    /// <summary>Enemy intent visibility: all rounds / first round only / hidden
    /// (default: always visible, user change 2026-09-21).</summary>
    public IntentVisibility IntentMode { get; set; } = IntentVisibility.All;

    /// <summary>Show real enemy models instead of the breathing-box mask
    /// (default on, user change 2026-09-21).</summary>
    public bool EnemyModelsVisible { get; set; } = true;

    public static int ClampBlurPercent(int value) => Math.Clamp(value, 0, 100);

    public static int ClampBadMemoryThreshold(int value) => Math.Clamp(value, 1, 99);

    /// <summary>Factory defaults of the current build (used when the player has
    /// not saved their own defaults yet).</summary>
    private static readonly DifficultySettings FactoryDefaults = new();

    /// <summary>Copies every option from another settings instance (used for
    /// the "save current as default" button and for resetting).</summary>
    public void CopyFrom(DifficultySettings other)
    {
        TextBlurPercent = other.TextBlurPercent;
        SaltMode = other.SaltMode;
        SelectionReveal = other.SelectionReveal;
        RevealShopAndEventCards = other.RevealShopAndEventCards;
        MemoryMode = other.MemoryMode;
        BadMemoryThreshold = other.BadMemoryThreshold;
        MemoryFade = other.MemoryFade;
        SnapshotStatus = other.SnapshotStatus;
        ReadableStatusNumbers = other.ReadableStatusNumbers;
        ShowRelics = other.ShowRelics;
        ShowAllMapRoutes = other.ShowAllMapRoutes;
        IntentMode = other.IntentMode;
        EnemyModelsVisible = other.EnemyModelsVisible;
    }

    /// <summary>Restores every option to the factory default (fallback when the
    /// player never saved custom defaults).</summary>
    public void ApplyDefaults() => CopyFrom(FactoryDefaults);

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
