using System.Collections.Generic;
using System.Linq;
using BrainFog.Core.Options;
using BrainFog.Core.Reveal;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace BrainFog.Game;

/// <summary>
/// "Memory fade" (user change 2026-09-21): at the end of every combat, cards
/// that are still unrevealed are removed from the run deck. Runs on the win
/// teardown (<see cref="Player.AfterCombatEnd"/>) so the removal is part of the
/// save the game writes right after combat. Nonsense mode is exempt. Removal
/// goes through the game's own CardPileCmd.RemoveFromDeck, so the original
/// remove animation/VFX and the BeforeCardRemoved hooks fire.
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

        RemoveFromDeck(player, removed, ids);
    }

    /// <summary>
    /// The game's own removal flow (history entry, BeforeCardRemoved hooks,
    /// card preview + NCardRemoveVfx animation, RemoveFromState). The hook
    /// listeners used in the base game complete synchronously, so the deck is
    /// updated before the post-combat save; if a listener ever yields, the
    /// bookkeeping is deferred to the main thread once removal completed.
    /// </summary>
    private static void RemoveFromDeck(Player player, List<CardModel> cards, List<string> ids)
    {
        var task = CardPileCmd.RemoveFromDeck(cards, showPreview: true);
        if (task.IsCompleted)
        {
            Finish(player, cards.Count, ids);
            return;
        }

        task.ContinueWith(_ =>
            Callable.From(() => PatchGuard.Run("MemoryFade.Finish", () => Finish(player, cards.Count, ids)))
                .CallDeferred());
    }

    private static void Finish(Player player, int count, List<string> ids)
    {
        RevealPersistence.OnCardsRemoved(player, ids);
        PlayCounterTracker.OnCardsRemoved(ids);
        foreach (var id in ids)
        {
            ModRuntime.Tracker.HideInstance(id);
        }

        Log.Info($"[BrainFog][MemoryFade] removed {count} unrevealed card(s) from the deck");
    }
}
