using BrainFog.Core.Text;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace BrainFog.Patches;

/// <summary>
/// Unified hover-tip blur policy (user rules 2026-09-19):
/// - card-view / upgrade screens: keyword descriptions 50%
/// - potion slots / popup / merchant: titles+descriptions 50%
/// - top bar / map / legend / share UI: titles+descriptions 70%
/// - everything else (unspecified): titles+descriptions 60%
/// Exempt: settings screens and the card compendium (stay readable).
/// </summary>
[HarmonyPatch(typeof(NHoverTipSet), "Init")]
internal static class HoverTipTextBlurPatch
{
    private enum Policy
    {
        Skip,
        CardViewDescriptions,
        Potion,
        UiDescriptions,
        Default,
    }

    [HarmonyPostfix]
    private static void Postfix(NHoverTipSet __instance)
    {
        try
        {
            if (ModRuntime.Disabled || __instance._textHoverTipContainer == null
                || !GodotObject.IsInstanceValid(__instance._textHoverTipContainer))
            {
                return;
            }

            var policy = ResolvePolicy(__instance);
            if (policy == Policy.Skip)
            {
                return;
            }

            var both = policy != Policy.CardViewDescriptions;
            var percent = policy switch
            {
                Policy.CardViewDescriptions => TextBlurPercents.CardViewTips,
                Policy.Potion => TextBlurPercents.Potion,
                Policy.UiDescriptions => TextBlurPercents.UiDescription,
                _ => TextBlurPercents.Default,
            };

            foreach (var child in __instance._textHoverTipContainer.GetChildren())
            {
                if (child is not Control tip)
                {
                    continue;
                }
                if (both)
                {
                    Blur(tip.GetNodeOrNull<Label>("%Title"), percent);
                }
                Blur(tip.GetNodeOrNull<RichTextLabel>("%Description"), percent);
            }
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("HoverTips.Blur", () => throw ex);
        }
    }

    private static void Blur(Control? label, int percent) =>
        Game.TextBlurService.BlurNode(label, percent);

    private static Policy ResolvePolicy(NHoverTipSet set)
    {
        var inCardView = false;
        var inTopBar = false;
        var inMapUi = false;
        var inPotion = false;

        for (Node? node = set._owner; node != null; node = node.GetParent())
        {
            var typeName = node.GetType().Name;
            if (typeName is "NSettingsScreen" or "NCardLibrary")
            {
                return Policy.Skip;
            }
            if (typeName is "NPotionHolder" or "NPotionPopup" or "NMerchantPotion")
            {
                inPotion = true;
            }
            if (typeName is "NInspectCardScreen" or "NDeckUpgradeSelectScreen" or "NUpgradePreview")
            {
                inCardView = true;
            }
            if (typeName.StartsWith("NTopBar", StringComparison.Ordinal))
            {
                inTopBar = true;
            }
            if (typeName is "NMapScreen" or "NMapLegendItem" or "NMapShareButton"
                or "NShareButton" or "NShareStatsButton" or "NBossMapPoint" or "NMapPoint"
                or "NNormalMapPoint" or "NAncientMapPoint")
            {
                inMapUi = true;
            }
        }

        if (inCardView)
        {
            return Policy.CardViewDescriptions;
        }
        if (inPotion)
        {
            return Policy.Potion;
        }
        if (inTopBar || inMapUi)
        {
            return Policy.UiDescriptions;
        }
        return Policy.Default;
    }
}
