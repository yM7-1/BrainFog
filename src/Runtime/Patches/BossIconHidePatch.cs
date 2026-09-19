using HarmonyLib;
using MegaCrit.sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace BrainFog.Patches;

/// <summary>
/// The top-bar act-boss icon ("boss, the mightiest enemy of the region") is
/// removed entirely (user change 2026-09-19): never rendered, never focusable,
/// its hover tip never shown. The map's own boss point is untouched.
/// </summary>
[HarmonyPatch(typeof(NTopBarBossIcon))]
internal static class BossIconHidePatch
{
    [HarmonyPatch("_Ready")]
    [HarmonyPostfix]
    private static void AfterReady(NTopBarBossIcon __instance) => Hide(__instance);

    [HarmonyPatch("OnActEntered")]
    [HarmonyPostfix]
    private static void AfterActEntered(NTopBarBossIcon __instance) => Hide(__instance);

    [HarmonyPatch("OnRoomEntered")]
    [HarmonyPrefix]
    private static bool BeforeRoomEntered(NTopBarBossIcon __instance)
    {
        Hide(__instance);
        return ModRuntime.Disabled;
    }

    [HarmonyPatch("RefreshBossIcon")]
    [HarmonyPrefix]
    private static bool BeforeRefreshBossIcon(NTopBarBossIcon __instance)
    {
        Hide(__instance);
        return ModRuntime.Disabled;
    }

    [HarmonyPatch("OnFocus")]
    [HarmonyPostfix]
    private static void AfterFocus(NTopBarBossIcon __instance) =>
        Game.PatchGuard.Run("BossIcon.Tip", () =>
        {
            if (!ModRuntime.Disabled)
            {
                NHoverTipSet.Remove(__instance);
            }
        });

    private static void Hide(NTopBarBossIcon icon)
    {
        try
        {
            if (ModRuntime.Disabled || !Godot.GodotObject.IsInstanceValid(icon))
            {
                return;
            }

            icon.Visible = false;
            icon.MouseFilter = Godot.Control.MouseFilterEnum.Ignore;
            icon.FocusMode = Godot.Control.FocusModeEnum.None;
            HideChild(icon, "Icon");
            HideChild(icon, "Icon/Outline");
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("BossIcon.Hide", () => throw ex);
        }
    }

    private static void HideChild(NTopBarBossIcon icon, string path)
    {
        var node = icon.GetNodeOrNull<Godot.Node>(path);
        if (node is Godot.CanvasItem item && Godot.GodotObject.IsInstanceValid(item))
        {
            item.Visible = false;
        }
    }
}
