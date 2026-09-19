using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BrainFog.Patches;

/// <summary>
/// The local player's combat health bar must not show true HP (leak fix
/// 2026-09-19): the HP number and bar fill are hidden, the top bar keeps
/// showing the stale snapshot (spec 0.02 #3). Block UI stays visible.
/// </summary>
[HarmonyPatch(typeof(NHealthBar))]
internal static class PlayerHpBarHidePatch
{
    private static void HideLocalPlayerHp(NHealthBar bar)
    {
        try
        {
            if (ModRuntime.Disabled
                || bar._creature is not { } creature
                || !creature.IsPlayer
                || !LocalContext.IsMe(creature))
            {
                return;
            }

            if (Game.DifficultyRuntime.Current.ShowLiveStatus)
            {
                return; // real-time display: let the game manage the bar
            }

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
        catch (Exception ex)
        {
            Game.PatchGuard.Run("PlayerHpBar.Hide", () => throw ex);
        }
    }

    [HarmonyPatch("RefreshValues")]
    [HarmonyPostfix]
    private static void AfterRefreshValues(NHealthBar __instance) => HideLocalPlayerHp(__instance);

    [HarmonyPatch("RefreshForeground")]
    [HarmonyPostfix]
    private static void AfterRefreshForeground(NHealthBar __instance) => HideLocalPlayerHp(__instance);

    [HarmonyPatch("RefreshMiddleground")]
    [HarmonyPostfix]
    private static void AfterRefreshMiddleground(NHealthBar __instance) => HideLocalPlayerHp(__instance);

    [HarmonyPatch("RefreshText")]
    [HarmonyPostfix]
    private static void AfterRefreshText(NHealthBar __instance) => HideLocalPlayerHp(__instance);
}
