using BrainFog.Core.Reveal;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace BrainFog.Game;

/// <summary>
/// Runtime scope checks for multiplayer (plan A): only local cards/players are
/// obfuscated, tracked or persisted. Remote entities keep the vanilla display.
/// </summary>
internal static class LocalScope
{
    public static bool IsRemoteCard(CardModel? card)
    {
        if (!ModRuntime.IsMultiplayer || card?.Owner is not { } owner)
        {
            return false;
        }
        return ScopeRules.IsRemoteCard(
            isMultiplayer: true,
            hasOwner: true,
            isLocalOwner: LocalContext.IsMe(owner));
    }

    public static bool IsRemotePlayer(Player? player)
    {
        if (!ModRuntime.IsMultiplayer || player == null)
        {
            return false;
        }
        return ScopeRules.IsRemotePlayer(
            isMultiplayer: true,
            hasPlayer: true,
            isLocalPlayer: LocalContext.IsMe(player));
    }

    /// <summary>True when this client must not record knowledge for the card.</summary>
    public static bool IsTrackable(CardModel? card) => !IsRemoteCard(card);
}
