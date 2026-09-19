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
}
