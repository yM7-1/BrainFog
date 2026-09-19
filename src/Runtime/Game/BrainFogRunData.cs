namespace BrainFog.Game;

/// <summary>
/// Per-run mod data persisted with the save file (RitsuLib RunSavedDataStore).
/// RevealedCards holds card definition keys ("cards.STRIKE"); base and upgraded
/// copies of the same card share the key, so no instance rebinding is needed.
/// </summary>
public sealed class BrainFogRunData
{
    public List<string> RevealedCards = new();
}
