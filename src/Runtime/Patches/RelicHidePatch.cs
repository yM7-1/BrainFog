using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Rewards;

namespace BrainFog.Patches;

/// <summary>Relics are invisible everywhere (spec 0.01 3.1 / 0.02 #9).</summary>
[HarmonyPatch(typeof(NRelic), "Reload")]
internal static class RelicHidePatch
{
    [HarmonyPostfix]
    private static void Postfix(NRelic __instance)
    {
        if (ModRuntime.Disabled || IsCompendiumEntry(__instance))
        {
            return;
        }
        if (__instance.Icon != null && GodotObject.IsInstanceValid(__instance.Icon))
        {
            __instance.Icon.Visible = false;
        }
        if (__instance.Outline != null && GodotObject.IsInstanceValid(__instance.Outline))
        {
            __instance.Outline.Visible = false;
        }
    }
    private static bool IsCompendiumEntry(NRelic relic)
    {
        var node = relic.GetParent();
        while (node != null)
        {
            if (node.GetType().Name.StartsWith("NRelicCollection", StringComparison.Ordinal))
            {
                return true;
            }
            node = node.GetParent();
        }
        return false;
    }
}

/// <summary>Relic rewards carry their own TextureRect (does not use NRelic).</summary>
[HarmonyPatch(typeof(RelicReward), "CreateIcon")]
internal static class RelicRewardHidePatch
{
    [HarmonyPostfix]
    private static void Postfix(TextureRect __result)
    {
        if (!ModRuntime.Disabled && __result != null && GodotObject.IsInstanceValid(__result))
        {
            __result.Visible = false;
        }
    }
}

/// <summary>Relic reward rows print the relic title as their label; replace it
/// with "unknown relic" while masked (leak fix 2026-09-19).</summary>
[HarmonyPatch(typeof(NRewardButton), "Reload")]
internal static class RelicRewardLabelPatch
{
    [HarmonyPostfix]
    private static void Postfix(NRewardButton __instance)
    {
        try
        {
            if (!ModRuntime.Disabled && __instance.Reward is RelicReward && __instance._label != null)
            {
                __instance._label.Text = Game.ModLocalization.UnknownRelic;
            }
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("RelicReward.Label", () => throw ex);
        }
    }
}

/// <summary>Relic inspect screen is covered by black fog (spec 0.01 3.1).</summary>
[HarmonyPatch(typeof(NInspectRelicScreen), "UpdateRelicDisplay")]
internal static class InspectRelicFogPatch
{
    [HarmonyPostfix]
    private static void Postfix(NInspectRelicScreen __instance)
    {
        if (ModRuntime.Disabled)
        {
            return;
        }

        if (__instance._relicImage != null && GodotObject.IsInstanceValid(__instance._relicImage))
        {
            __instance._relicImage.SelfModulate = Colors.Black;
        }
        Hide(__instance._nameLabel);
        Hide(__instance._description);
        Hide(__instance._flavor);
        Hide(__instance._rarityLabel);
    }

    private static void Hide(CanvasItem? node)
    {
        if (node != null && GodotObject.IsInstanceValid(node))
        {
            node.Visible = false;
        }
    }
}
