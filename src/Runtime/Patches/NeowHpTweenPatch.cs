using BrainFog.Core.Status;
using HarmonyLib;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace BrainFog.Patches;

/// <summary>
/// Neow's heal tween writes the true value straight into the top bar. It is
/// suppressed only in "HP/gold amnesia mode" (stale snapshot, spec 0.02 #3);
/// in live mode (the default since 0.3.2) the original tween must run or the
/// bar shows "0/max" for the whole Neow event (0.3.7 fix).
/// </summary>
[HarmonyPatch(typeof(NTopBarHp), "UpdateHpTween")]
internal static class NeowHpTweenPatch
{
    [HarmonyPrefix]
    private static bool Prefix() => StatusDisplayRules.AllowsLiveHpUpdates(
        ModRuntime.Disabled, Game.DifficultyRuntime.Current.SnapshotStatus);
}
