using System.Diagnostics;
using BrainFog.Core.Options;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace BrainFog.Patches;

/// <summary>
/// Playing a card reveals knowledge according to the memory mode (user change
/// 2026-09-21):
/// - "good memory": the whole card definition is revealed for the run (every
///   copy, including upgraded ones, and acquisition screens);
/// - "bad memory": only the played copy is revealed (persisted by deck order);
/// - "omniscient" / "nonsense": nothing to record.
/// The prefix also marks the card as played for the bad-memory counter.
/// Work only happens on the first reveal of the scope; repeated plays are a
/// cheap no-op (perf: card-play hitch fix 2026-09-20). Visual refresh is
/// deferred one frame so it never competes with the play animation start.
/// </summary>
[HarmonyPatch(typeof(CardModel), "OnPlayWrapper")]
internal static class CardModelPlayRevealPatch
{
    [HarmonyPrefix]
    private static void Prefix(CardModel __instance)
    {
        if (!ModRuntime.Disabled)
        {
            Game.BadMemoryTracker.OnPlayed(__instance);
        }
    }

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
        switch (Game.DifficultyRuntime.Current.MemoryMode)
        {
            case CardMemoryMode.Omniscient:
            case CardMemoryMode.Nonsense:
                return false;
            case CardMemoryMode.BadMemory:
            {
                var id = Game.CardInstanceRegistry.GetOrCreateId(card);
                if (string.IsNullOrEmpty(id) || !ModRuntime.Tracker.RevealInstanceByPlay(id))
                {
                    return false;
                }
                Game.RevealPersistence.OnInstanceRevealed(card, id);
                return true;
            }
            default:
            {
                var key = Game.RevealKeys.Of(card);
                if (!ModRuntime.Tracker.RevealByPlay(key))
                {
                    return false; // already known: nothing to persist or refresh
                }
                Game.RevealPersistence.OnRevealed(key);
                return true;
            }
        }
    }
}
