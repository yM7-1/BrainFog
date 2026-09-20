using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace BrainFog.Patches;

/// <summary>
/// Feeds the "bad memory" counters (user change 2026-09-21): entering the hand
/// marks a new entry, leaving it without a play counts one unplayed draw.
/// Covers natural draws and every effect that moves a card back to hand.
/// </summary>
[HarmonyPatch(typeof(CardPile), "AddInternal")]
internal static class CardPileHandAddPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardPile __instance, CardModel card)
    {
        if (__instance.Type == PileType.Hand)
        {
            Game.BadMemoryTracker.OnEnterHand(card);
        }
    }
}

[HarmonyPatch(typeof(CardPile), "RemoveInternal")]
internal static class CardPileHandRemovePatch
{
    [HarmonyPostfix]
    private static void Postfix(CardPile __instance, CardModel card)
    {
        if (__instance.Type == PileType.Hand)
        {
            Game.BadMemoryTracker.OnLeaveHand(card);
        }
    }
}
