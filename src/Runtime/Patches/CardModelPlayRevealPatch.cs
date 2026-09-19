using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BrainFog.Patches;

/// <summary>
/// Playing a card once reveals that card for the rest of the run — every copy,
/// not just the played instance (user change 2026-09-19) — and the reveal is
/// persisted with the run save.
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

        var key = Game.RevealKeys.Of(__instance);
        ModRuntime.Tracker.RevealByPlay(key);
        Game.RevealPersistence.OnRevealed(key);
        Game.CardFogRenderer.RefreshLiveCards(__instance);
    }
}
