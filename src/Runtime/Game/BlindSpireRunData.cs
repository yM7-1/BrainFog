namespace BlindSpire.Game;

/// <summary>
/// Per-run mod data persisted with the save file (RitsuLib RunSavedDataStore).
/// DeckOrderIds is index-aligned with the player's deck order at save time,
/// which lets us rebind instance ids after a load (the game has no card instance ids).
/// </summary>
public sealed class BlindSpireRunData
{
    public List<string> RevealedIds = new();

    public List<string> DeckOrderIds = new();
}
