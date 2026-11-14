namespace BrainFog.Core.Reveal;

/// <summary>
/// Multiplayer "only blind yourself" scope decisions (plan A). Remote players
/// and their cards must stay at the vanilla presentation; only the local
/// player's knowledge and deck-limited effects are in scope.
/// </summary>
public static class ScopeRules
{
    /// <summary>True when a card belongs to another player and must not be
    /// hidden, tracked, remembered or removed by this client.</summary>
    public static bool IsRemoteCard(bool isMultiplayer, bool hasOwner, bool isLocalOwner) =>
        isMultiplayer && hasOwner && !isLocalOwner;

    /// <summary>True when a player (or that player's creature/pet) is remote.</summary>
    public static bool IsRemotePlayer(bool isMultiplayer, bool hasPlayer, bool isLocalPlayer) =>
        isMultiplayer && hasPlayer && !isLocalPlayer;
}
