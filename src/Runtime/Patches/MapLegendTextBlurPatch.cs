using BrainFog.Core.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.addons.mega_text;

namespace BrainFog.Patches;

/// <summary>
/// Map legend labels (header and enemy/merchant/treasure/rest/elite/boss/...)
/// follow the unified blur ratio by default. Since 0.3.4 the "show all map
/// routes" option restores the original legend text instead. The hover
/// descriptions (including the boss's "mightiest enemy of the region") follow
/// the unified ratio again since 2026-09-21 (see
/// <see cref="HoverTipTextBlurPatch"/>).
/// </summary>
[HarmonyPatch(typeof(NMapLegendItem), "SetLocalizedFields")]
internal static class MapLegendTextBlurPatch
{
    [HarmonyPostfix]
    private static void Postfix(NMapLegendItem __instance)
    {
        var label = __instance.GetNodeOrNull<MegaLabel>("MegaLabel");
        ApplyLabel(label);
    }

    /// <summary>Blur or restore one legend label per the current option.</summary>
    internal static void ApplyLabel(MegaLabel? label)
    {
        try
        {
            if (label == null || !Godot.GodotObject.IsInstanceValid(label))
            {
                return;
            }
            if (ModRuntime.Disabled || Game.DifficultyRuntime.Current.ShowAllMapRoutes)
            {
                Game.TextBlurService.Restore(label);
                return;
            }
            Game.TextBlurService.BlurNode(label, Game.DifficultyRuntime.TextBlurPercent);
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("MapLegend.Blur", () => throw ex);
        }
    }

    /// <summary>Option toggled: refresh the header and every legend entry.</summary>
    internal static void Refresh(NMapScreen screen)
    {
        try
        {
            if (!Godot.GodotObject.IsInstanceValid(screen))
            {
                return;
            }

            ApplyLabel(screen.GetNodeOrNull<MegaLabel>("MapLegend/Header"));
            if (screen._legendItems == null || !Godot.GodotObject.IsInstanceValid(screen._legendItems))
            {
                return;
            }
            foreach (var child in screen._legendItems.GetChildren())
            {
                if (child is NMapLegendItem item)
                {
                    ApplyLabel(item.GetNodeOrNull<MegaLabel>("MegaLabel"));
                }
            }
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("MapLegend.Refresh", () => throw ex);
        }
    }
}
