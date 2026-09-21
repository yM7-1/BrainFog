using BrainFog.Core.Options;

namespace BrainFog.Core.Reveal;

/// <summary>
/// "Memory fade" (user change 2026-09-21): a card that is still unrevealed at
/// the end of a combat is removed from the deck. Good-memory (definition scope)
/// and bad-memory (copy scope) are affected; nonsense mode is exempt because its
/// cards are never revealed, and omniscience has nothing unrevealed to remove.
/// </summary>
public static class MemoryFadeRules
{
    public static bool Applies(CardMemoryMode mode) =>
        mode is CardMemoryMode.GoodMemory or CardMemoryMode.BadMemory;

    /// <summary>True when the card is unrevealed ("失忆") for the given mode.</summary>
    public static bool IsUnrevealed(
        CardMemoryMode mode,
        bool definitionRevealed,
        bool instanceRevealed) => mode switch
        {
            CardMemoryMode.GoodMemory => !definitionRevealed,
            CardMemoryMode.BadMemory => !instanceRevealed,
            _ => false,
        };
}
