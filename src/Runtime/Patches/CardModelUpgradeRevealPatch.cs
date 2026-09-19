using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BrainFog.Patches;

/// <summary>
/// Any upgrade (smith, events, relics) reveals the true face even if never played
/// (spec 0.03 a); like play reveals, this covers every copy of the card for the run.
/// Only cards that live in one of the player's piles count: grid/hover-tip upgrade
/// previews and save deserialization upgrade clones (leak fix 2026-09-19) must not
/// hand out knowledge.
/// </summary>
[HarmonyPatch(typeof(CardModel), "set_CurrentUpgradeLevel")]
internal static class CardModelUpgradeRevealPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance)
    {
        if (ModRuntime.Disabled || __instance.CurrentUpgradeLevel <= 0)
        {
            return;
        }

        if (__instance.Pile == null)
        {
            return; // preview clone / not part of the run deck
        }

        var key = Game.RevealKeys.Of(__instance);
        ModRuntime.Tracker.RevealByUpgrade(key);
        Game.RevealPersistence.OnRevealed(key);
        Game.CardFogRenderer.RefreshLiveCards(__instance);
    }
}
