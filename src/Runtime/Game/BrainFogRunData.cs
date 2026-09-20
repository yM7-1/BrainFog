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
}
