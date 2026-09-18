using BlindSpire.Core.Reveal;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace BlindSpire.Patches;

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
