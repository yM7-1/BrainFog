using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BrainFog.Patches;

/// <summary>
/// Snapshot mode only (panel option, default off): the local player's combat
/// health bar hides true HP (number and fill), the top bar keeps the snapshot.
/// Default (live) lets the game show the bar; its numbers are garbled by the
/// unified blur ratio. Block UI stays visible. Turning the snapshot option off
/// or disabling the mod restores the bar (0.3.7).
/// </summary>
[HarmonyPatch(typeof(NHealthBar))]
internal static class PlayerHpBarHidePatch
{
    [HarmonyPatch("RefreshValues")]
    [HarmonyPostfix]
    private static void AfterRefreshValues(NHealthBar __instance) => PlayerHpBarMask.Apply(__instance);

    [HarmonyPatch("RefreshForeground")]
    [HarmonyPostfix]
    private static void AfterRefreshForeground(NHealthBar __instance) => PlayerHpBarMask.Apply(__instance);

    [HarmonyPatch("RefreshMiddleground")]
    [HarmonyPostfix]
    private static void AfterRefreshMiddleground(NHealthBar __instance) => PlayerHpBarMask.Apply(__instance);

    [HarmonyPatch("RefreshText")]
    [HarmonyPostfix]
    private static void AfterRefreshText(NHealthBar __instance) => PlayerHpBarMask.Apply(__instance);
}

internal static class PlayerHpBarMask
{
    /// <summary>Marks a bar whose true-HP parts this mod hid.</summary>
    public const string HiddenMeta = "BrainFogHpHidden";

    public static void Apply(NHealthBar bar)
    {
        try
        {
            if (!GodotObject.IsInstanceValid(bar))
            {
                return;
            }

            var shouldHide = !ModRuntime.Disabled
                && Game.DifficultyRuntime.Current.SnapshotStatus
                && bar._creature is { IsPlayer: true } creature
                && LocalContext.IsMe(creature);

            if (shouldHide)
            {
                Hide(bar);
                bar.SetMeta(HiddenMeta, true);
                return;
            }

            // Restore: the game's own Refresh* methods re-show exactly what
            // vanilla shows (called by RefreshValues before this postfix).
            if (bar.HasMeta(HiddenMeta))
            {
                bar.RemoveMeta(HiddenMeta);
            }
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("PlayerHpBar.Hide", () => throw ex);
        }
    }

    private static void Hide(NHealthBar bar)
    {
        bar._hpLabel.Visible = false;
        bar._hpForeground.Visible = false;
        bar._hpMiddleground.Visible = false;
        bar._poisonForeground.Visible = false;
        bar._doomForeground.Visible = false;
        if (bar._infinityTex != null)
        {
            bar._infinityTex.Visible = false;
        }
    }
}
