using System.Collections.Generic;
using System.Linq;
using BrainFog.Core.Options;
using BrainFog.Core.Reveal;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace BrainFog.Game;

/// <summary>
/// "Memory fade" (user change 2026-09-21): at the end of every combat, cards
/// that are still unrevealed are removed from the run deck. Runs on the win
/// teardown (<see cref="Player.AfterCombatEnd"/>) so the removal is part of the
/// save the game writes right after combat. Nonsense mode is exempt.
/// </summary>
internal static class MemoryFade
{
    public static void OnCombatEnded(Player player) =>
        PatchGuard.Run("MemoryFade", () => OnCombatEndedCore(player));

    private static void OnCombatEndedCore(Player player)
    {
        if (ModRuntime.Disabled || !DifficultyRuntime.Current.MemoryFade)
        {
            return;
        }

        var mode = DifficultyRuntime.Current.MemoryMode;
        if (!MemoryFadeRules.Applies(mode))
        {
            return;
        }

        var removed = new List<CardModel>();
        var ids = new List<string>();
        foreach (var card in player.Deck.Cards.ToArray())
        {
            var id = CardInstanceRegistry.TryGetId(card);
            var unrevealed = MemoryFadeRules.IsUnrevealed(
                mode,
                definitionRevealed: ModRuntime.Tracker.IsRevealed(RevealKeys.Of(card)),
                instanceRevealed: !string.IsNullOrEmpty(id) && ModRuntime.Tracker.IsInstanceRevealed(id));
            if (!unrevealed)
            {
                continue;
            }

            removed.Add(card);
            if (!string.IsNullOrEmpty(id))
            {
                ids.Add(id);
            }
        }

        if (removed.Count == 0)
        {
            return;
        }

        foreach (var card in removed)
        {
            RemoveFromDeck(player, card);
        }

        RevealPersistence.OnCardsRemoved(player, ids);
        PlayCounterTracker.OnCardsRemoved(ids);
        foreach (var id in ids)
        {
            ModRuntime.Tracker.HideInstance(id);
        }

        Log.Info($"[BrainFog][MemoryFade] removed {removed.Count} unrevealed card(s) from the deck");
    }

    /// <summary>Same sequence the game's CardPileCmd.RemoveFromDeck performs,
    /// without the preview VFX (the deck changes during combat teardown).</summary>
    private static void RemoveFromDeck(Player player, CardModel card)
    {
        player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId).CardsRemoved.Add(card.ToSerializable());
        card.RemoveFromCurrentPile();
        card.RemoveFromState();
    }
}
