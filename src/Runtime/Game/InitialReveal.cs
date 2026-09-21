using System.Collections.Generic;
using BrainFog.Core.Options;
using MegaCrit.Sts2.Core.Runs;

namespace BrainFog.Game;

/// <summary>
/// Fresh run (user change 2026-09-21): the starting deck counts as known.
/// Good memory reveals the definitions for the run, bad memory reveals the
/// copies (they can still be forgotten later); omniscience needs nothing and
/// nonsense stays never-revealed. Also assigns the play-counter numbers for
/// every starting-deck copy (stable numbering).
/// </summary>
internal static class InitialReveal
{
    public static void Apply(RunState state) =>
        PatchGuard.Run("InitialReveal", () => ApplyCore(state));

    private static void ApplyCore(RunState state)
    {
        var mode = DifficultyRuntime.Current.MemoryMode;
        foreach (var player in state.Players)
        {
            var keys = new List<string>();
            var ids = new List<string>();
            foreach (var card in player.Deck.Cards)
            {
                if (mode == CardMemoryMode.BadMemory)
                {
                    var id = CardInstanceRegistry.GetOrCreateId(card);
                    if (!string.IsNullOrEmpty(id) && ModRuntime.Tracker.RevealInstanceByPlay(id))
                    {
                        ids.Add(id);
                    }
                }
                else if (mode == CardMemoryMode.GoodMemory)
                {
                    var key = RevealKeys.Of(card);
                    if (ModRuntime.Tracker.RevealByPlay(key))
                    {
                        keys.Add(key);
                    }
                }
            }

            RevealPersistence.OnInitialReveal(player, keys, ids);
        }

        PlayCounterTracker.EnsureDeckNumbers(state);
        CardFogRenderer.RefreshAllLiveCards();
    }
}
