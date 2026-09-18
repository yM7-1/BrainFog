using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BlindSpire.Patches;

/// <summary>
/// Playing an instance once reveals its face for the rest of the run (spec 0.02 #6),
/// and the reveal is persisted with the run save.
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

        var id = Game.CardInstanceRegistry.GetOrCreateId(__instance);
        ModRuntime.Tracker.RevealByPlay(id);
        Game.RevealPersistence.OnRevealed(__instance, id);
        Game.CardFogRenderer.RefreshLiveCards(__instance);
    }
}
