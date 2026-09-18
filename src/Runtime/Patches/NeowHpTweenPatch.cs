using HarmonyLib;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace BlindSpire.Patches;

/// <summary>
/// Neow's HP-only show writes the true value straight into the top bar; keep the
/// stale snapshot instead (spec 0.02 #3).
/// </summary>
[HarmonyPatch(typeof(NTopBarHp), "UpdateHpTween")]
internal static class NeowHpTweenPatch
{
    [HarmonyPrefix]
    private static bool Prefix() => ModRuntime.Disabled;
}
