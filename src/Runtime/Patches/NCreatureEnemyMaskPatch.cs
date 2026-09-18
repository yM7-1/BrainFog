using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BlindSpire.Patches;

/// <summary>Enemy models are masked at creature creation (spec 0.01 4.1).</summary>
[HarmonyPatch(typeof(NCreature), "_Ready")]
internal static class NCreatureEnemyMaskPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCreature __instance)
    {
        Game.EnemyVisualMask.Apply(__instance);
    }
}
