using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Rewards;

namespace BrainFog.Patches;

/// <summary>
/// Relics are invisible by default; the difficulty option "show owned relics"
/// keeps relics in the player's own inventory/inspect view visible.
/// </summary>
internal static class RelicMasking
{
    public static void Apply(NRelic relic)
    {
        if (ModRuntime.Disabled || !GodotObject.IsInstanceValid(relic) || IsCompendiumEntry(relic))
        {
            return;
        }

        var show = Game.DifficultyRuntime.Current.ShowOwnedRelics && IsOwnedContext(relic);
        SetVisible(relic.Icon, show);
        SetVisible(relic.Outline, show);
    }

    private static bool IsOwnedContext(NRelic relic)
    {
        for (var node = relic.GetParent(); node != null; node = node.GetParent())
        {
            if (node is NRelicInventory)
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsCompendiumEntry(NRelic relic)
    {
        for (var node = relic.GetParent(); node != null; node = node.GetParent())
        {
            if (node.GetType().Name.StartsWith("NRelicCollection", StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static void SetVisible(CanvasItem? item, bool visible)
    {
        if (item != null && GodotObject.IsInstanceValid(item))
        {
            item.Visible = visible;
        }
    }
}

[HarmonyPatch(typeof(NRelic), "Reload")]
internal static class RelicHidePatch
{
    [HarmonyPostfix]
    private static void Postfix(NRelic __instance) => RelicMasking.Apply(__instance);
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

/// <summary>Relic inspect screen is fogged by default; "show owned relics"
/// restores the real display.</summary>
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

        if (Game.DifficultyRuntime.Current.ShowOwnedRelics)
        {
            if (__instance._relicImage != null && GodotObject.IsInstanceValid(__instance._relicImage))
            {
                __instance._relicImage.SelfModulate = Colors.White;
            }
            Show(__instance._nameLabel);
            Show(__instance._description);
            Show(__instance._flavor);
            Show(__instance._rarityLabel);
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

    private static void Show(CanvasItem? node)
    {
        if (node != null && GodotObject.IsInstanceValid(node))
        {
            node.Visible = true;
        }
    }
}
