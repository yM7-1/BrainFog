using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BlindSpire.Patches;

/// <summary>
/// Any upgrade (smith, events, relics) reveals the true face even if never played (spec 0.03 a).
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

        var id = Game.CardInstanceRegistry.GetOrCreateId(__instance);
        ModRuntime.Tracker.RevealByUpgrade(id);
        Game.RevealPersistence.OnRevealed(__instance, id);
    }
}
