using System.Diagnostics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace BrainFog.Patches;

/// <summary>
/// Playing a card reveals knowledge according to the difficulty option:
/// - "same-name reveal" on (default): the whole card definition is revealed for
///   the run (every copy, including upgraded ones);
/// - off: only the played instance is revealed (persisted by deck order).
/// Work only happens on the first reveal of the scope; repeated plays are a
/// cheap no-op (perf: card-play hitch fix 2026-09-20). Visual refresh is
/// deferred one frame so it never competes with the play animation start.
/// </summary>
[HarmonyPatch(typeof(CardModel), "OnPlayWrapper")]
internal static class CardModelPlayRevealPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance)
    {
        if (ModRuntime.Disabled)
        {
            return;
        }

        var watch = ModRuntime.DebugEnabled ? Stopwatch.StartNew() : null;
        try
        {
            if (!RevealCore(__instance))
            {
                return;
            }

            var model = __instance;
            Godot.Callable.From(() => Game.CardFogRenderer.RefreshLiveCards(model)).CallDeferred();
        }
        finally
        {
            if (watch != null)
            {
                watch.Stop();
                var ms = watch.Elapsed.TotalMilliseconds;
                if (ms > 0.5)
                {
                    Log.Info($"[BrainFog][Perf] play-reveal {ms:F2} ms");
                }
            }
        }
    }

    /// <summary>Returns true when new knowledge was recorded (visuals need a refresh).</summary>
    private static bool RevealCore(CardModel card)
    {
        if (Game.DifficultyRuntime.Current.RevealSameNameCards)
        {
            var key = Game.RevealKeys.Of(card);
            if (!ModRuntime.Tracker.RevealByPlay(key))
            {
                return false; // already known: nothing to persist or refresh
            }
            Game.RevealPersistence.OnRevealed(key);
            return true;
        }

        var id = Game.CardInstanceRegistry.GetOrCreateId(card);
        if (string.IsNullOrEmpty(id) || !ModRuntime.Tracker.RevealInstanceByPlay(id))
        {
            return false;
        }
        Game.RevealPersistence.OnInstanceRevealed(card, id);
        return true;
    }
}
