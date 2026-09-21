namespace BrainFog.Game;

/// <summary>
/// Per-run mod data persisted with the save file (RitsuLib RunSavedDataStore).
/// - RevealedCards: definition keys ("cards.STRIKE"), used when "reveal
///   same-name cards" is on (default).
/// - RevealedInstances + DeckOrderIds: per-instance reveals, used when the
///   option is off; ids are rebound to deck slots by order after a load.
/// </summary>
public sealed class BrainFogRunData
{
    public List<string> RevealedCards = new();

    public List<string> RevealedInstances = new();

    public List<string> DeckOrderIds = new();

    /// <summary>Definition key per deck slot, parallel to <see cref="DeckOrderIds"/>;
    /// lets a load re-align ids after cards were added/removed (2026-09-21).</summary>
    public List<string> DeckOrderKeys = new();

    /// <summary>"Bad memory" mode counters per card copy (instance id → unplayed
    /// hand entries), persisted with the run (2026-09-21).</summary>
    public Dictionary<string, int> BadMemoryCounts = new();

    /// <summary>"Play counter" play count per card copy (instance id → count).</summary>
    public Dictionary<string, int> PlayCounts = new();

    /// <summary>"Play counter" stable copy numbers (instance id → 打击1/打击2).</summary>
    public Dictionary<string, int> CardNumbers = new();

    /// <summary>"Play counter" display names captured per copy (survive language
    /// changes imperfectly; live deck cards refresh theirs on render).</summary>
    public Dictionary<string, string> CardNames = new();

    /// <summary>"Play counter" next number per card definition, so numbers of
    /// copies removed from the deck are never reused (stable numbering).</summary>
    public Dictionary<string, int> NextCardNumbers = new();
}
