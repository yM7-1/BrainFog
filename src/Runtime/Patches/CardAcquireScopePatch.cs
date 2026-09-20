using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace BrainFog.Patches;

/// <summary>
/// Tags acquisition card-select screens before their cards are created
/// (2026-09-21): the mark is set on the screen node, and CardFogRenderer treats
/// cards inside as acquisition (rarity border only). This closes the event
/// card-gain deviation (IMPLEMENTATION-MAP §2.3): both commands generate cards
/// that are not in any player pile, and the mark keeps the rule explicit even
/// if a model carries a pile reference.
/// </summary>
[HarmonyPatch(typeof(NChooseACardSelectionScreen), "ShowScreen")]
internal static class ChooseACardScreenAcquireScopePatch
{
    [HarmonyPostfix]
    private static void Postfix(NChooseACardSelectionScreen? __result) =>
        Game.CardAcquireScope.Mark(__result);
}

[HarmonyPatch(
    typeof(NSimpleCardSelectScreen),
    "Create",
    new[] { typeof(IReadOnlyList<CardModel>), typeof(CardSelectorPrefs) })]
internal static class SimpleCardSelectAcquireScopePatch
{
    [HarmonyPostfix]
    private static void Postfix(NSimpleCardSelectScreen? __result) =>
        Game.CardAcquireScope.Mark(__result);
}

[HarmonyPatch(
    typeof(NSimpleCardSelectScreen),
    "Create",
    new[] { typeof(IReadOnlyList<CardCreationResult>), typeof(CardSelectorPrefs) })]
internal static class SimpleCardSelectRewardsAcquireScopePatch
{
    [HarmonyPostfix]
    private static void Postfix(NSimpleCardSelectScreen? __result) =>
        Game.CardAcquireScope.Mark(__result);
}
