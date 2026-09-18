using BlindSpire.Core.Reveal;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace BlindSpire.Patches;

/// <summary>
/// The deck viewer sorts cards by type/cost/alphabet on the REAL model, which
/// leaks unknown cards' properties by position (leak audit). When the deck holds
/// unknown instances, fall back to acquisition order. The compendium is exempt
/// (spec 0.03 j).
/// </summary>
[HarmonyPatch(typeof(NCardGrid), "SetCards")]
internal static class DeckSortLeakPatch
{
    [HarmonyPrefix]
    private static void Prefix(NCardGrid __instance, IReadOnlyList<CardModel> cardsToDisplay, ref List<SortingOrders> sortingPriority)
    {
        try
        {
            Sanitize(__instance, cardsToDisplay, ref sortingPriority);
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("SortLeak.Sanitize", () => throw ex);
        }
    }

    private static void Sanitize(NCardGrid grid, IReadOnlyList<CardModel> cards, ref List<SortingOrders> priority)
    {
        if (ModRuntime.Disabled || cards == null || priority == null || priority.Count == 0)
        {
            return;
        }
        if (!IsDeckViewer(grid) || !HasUnknownCard(cards))
        {
            return;
        }

        priority.RemoveAll(order => order is not (SortingOrders.Ascending or SortingOrders.Descending));
    }

    private static bool IsDeckViewer(NCardGrid grid)
    {
        var node = grid.GetParent();
        while (node != null)
        {
            if (node is NDeckViewScreen)
            {
                return true;
            }
            if (node is NCardLibrary)
            {
                return false;
            }
            node = node.GetParent();
        }
        return false;
    }

    private static bool HasUnknownCard(IReadOnlyList<CardModel> cards)
    {
        foreach (var card in cards)
        {
            if (card == null)
            {
                continue;
            }
            var id = Game.CardInstanceRegistry.GetOrCreateId(card);
            if (ModRuntime.Tracker.GetKnowledge(id) == CardKnowledge.Unknown)
            {
                return true;
            }
        }
        return false;
    }
}
