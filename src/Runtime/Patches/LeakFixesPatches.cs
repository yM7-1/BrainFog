using BrainFog.Core.Reveal;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace BrainFog.Patches;

/// <summary>
/// Unknown cards show no hover tips: real tips (keywords, mechanics) would leak
/// the card's identity (leak audit). Revealed cards keep vanilla tips.
/// </summary>
[HarmonyPatch(typeof(NCardHolder), "CreateHoverTips")]
internal static class HolderHoverTipFogPatch
{
    [HarmonyPrefix]
    private static bool Prefix(NCardHolder __instance) =>
        Game.PatchGuard.RunOr("HoverTips.Suppress", () => ShouldShowTips(__instance), true);

    private static bool ShouldShowTips(NCardHolder holder)
    {
        if (ModRuntime.Disabled)
        {
            return true;
        }

        var card = holder.CardNode;
        if (card?.Model == null)
        {
            return true;
        }
        return Game.CardFogRenderer.ResolveRule(card) == CardVisualRule.FullFace;
    }
}

/// <summary>
/// Unknown hand cards never glow as playable: the highlight would leak the
/// real energy cost (leak audit).
/// </summary>
[HarmonyPatch(typeof(NHandCardHolder), "UpdateCard")]
internal static class HandCardHighlightFogPatch
{
    [HarmonyPostfix]
    private static void Postfix(NHandCardHolder __instance) =>
        Game.PatchGuard.Run("HandHighlight.Suppress", () => Suppress(__instance));

    private static void Suppress(NHandCardHolder holder)
    {
        if (ModRuntime.Disabled)
        {
            return;
        }

        var card = holder.CardNode;
        if (card?.Model == null)
        {
            return;
        }
        if (Game.CardFogRenderer.ResolveRule(card) != CardVisualRule.FullFace)
        {
            card.CardHighlight.AnimHide();
        }
    }
}


/// <summary>
/// Event option focus tips can carry real card/relic details (non-Ancient events);
/// suppress while masked. Boss relic choices (Ancient events) keep their tips
/// (spec 0.03 i).
/// </summary>
[HarmonyPatch(typeof(NEventOptionButton), "OnFocus")]
internal static class EventOptionHoverTipPatch
{
    [HarmonyPostfix]
    private static void Postfix(NEventOptionButton __instance) =>
        Game.PatchGuard.Run("EventTips.OptionFocus", () =>
        {
            if (!ModRuntime.Disabled && !Game.AncientChoiceRules.StaysVisible(__instance.Event))
            {
                NHoverTipSet.Remove(__instance);
            }
        });
}


/// <summary>
/// Shop cards use their own hover tip path; suppress while masked (acquisition
/// shows the rarity border only, spec 0.02 #5).
/// </summary>
[HarmonyPatch(typeof(NMerchantCard), "CreateHoverTip")]
internal static class MerchantCardHoverTipPatch
{
    [HarmonyPrefix]
    private static bool Prefix(NMerchantCard __instance) =>
        Game.PatchGuard.RunOr("ShopTips.Suppress", () =>
        {
            if (ModRuntime.Disabled)
            {
                return true;
            }

            var card = __instance._cardNode;
            if (card?.Model == null)
            {
                return true;
            }
            return Game.CardFogRenderer.ResolveRule(card) == CardVisualRule.FullFace;
        }, true);
}
