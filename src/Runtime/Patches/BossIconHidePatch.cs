using Godot;
using HarmonyLib;
using MegaCrit.sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace BrainFog.Patches;

/// <summary>
/// The top-bar act-boss icon ("boss, the mightiest enemy of the region") is
/// hidden by default (user change 2026-09-19). Since 0.3.4 the difficulty
/// option "show all map routes" restores it completely: the icon, its focus and
/// its original hover description. The map's own boss point is owned by
/// <see cref="BossMapPointMaskPatch"/>.
/// </summary>
[HarmonyPatch(typeof(NTopBarBossIcon))]
internal static class BossIconHidePatch
{
    private static bool ShouldHide =>
        !ModRuntime.Disabled && !Game.DifficultyRuntime.Current.ShowAllMapRoutes;

    [HarmonyPatch("_Ready")]
    [HarmonyPostfix]
    private static void AfterReady(NTopBarBossIcon __instance)
    {
        if (ShouldHide)
        {
            Hide(__instance);
        }
    }

    [HarmonyPatch("OnActEntered")]
    [HarmonyPostfix]
    private static void AfterActEntered(NTopBarBossIcon __instance)
    {
        if (ShouldHide)
        {
            Hide(__instance);
        }
    }

    [HarmonyPatch("OnRoomEntered")]
    [HarmonyPrefix]
    private static bool BeforeRoomEntered(NTopBarBossIcon __instance)
    {
        if (!ShouldHide)
        {
            return true; // option on: the original decides visibility per room
        }
        Hide(__instance);
        return false;
    }

    [HarmonyPatch("RefreshBossIcon")]
    [HarmonyPrefix]
    private static bool BeforeRefreshBossIcon(NTopBarBossIcon __instance)
    {
        if (!ShouldHide)
        {
            return true;
        }
        Hide(__instance);
        return false;
    }

    [HarmonyPatch("OnFocus")]
    [HarmonyPostfix]
    private static void AfterFocus(NTopBarBossIcon __instance) =>
        Game.PatchGuard.Run("BossIcon.Tip", () =>
        {
            if (ShouldHide)
            {
                NHoverTipSet.Remove(__instance);
            }
        });

    /// <summary>Option toggled: restore the icon (vanilla room rules) or hide it.</summary>
    internal static void Refresh(NTopBarBossIcon icon) =>
        Game.PatchGuard.Run("BossIcon.Refresh", () =>
        {
            if (!GodotObject.IsInstanceValid(icon))
            {
                return;
            }
            if (ShouldHide)
            {
                Hide(icon);
                return;
            }

            Restore(icon);
            icon.OnRoomEntered();
            icon.RefreshBossIcon();
        });

    private static void Hide(NTopBarBossIcon icon)
    {
        try
        {
            if (!ShouldHide || !GodotObject.IsInstanceValid(icon))
            {
                return;
            }

            icon.Visible = false;
            icon.MouseFilter = Control.MouseFilterEnum.Ignore;
            icon.FocusMode = Control.FocusModeEnum.None;
            HideChild(icon, "Icon");
            HideChild(icon, "Icon/Outline");
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("BossIcon.Hide", () => throw ex);
        }
    }

    private static void Restore(NTopBarBossIcon icon)
    {
        icon.Visible = true;
        icon.MouseFilter = Control.MouseFilterEnum.Stop;
        icon.FocusMode = Control.FocusModeEnum.All;
        ShowChild(icon, "Icon");
        ShowChild(icon, "Icon/Outline");
    }

    private static void HideChild(NTopBarBossIcon icon, string path) => SetChildVisible(icon, path, false);

    private static void ShowChild(NTopBarBossIcon icon, string path) => SetChildVisible(icon, path, true);

    private static void SetChildVisible(NTopBarBossIcon icon, string path, bool visible)
    {
        var node = icon.GetNodeOrNull<Node>(path);
        if (node is CanvasItem item && GodotObject.IsInstanceValid(item))
        {
            item.Visible = visible;
        }
    }
}
