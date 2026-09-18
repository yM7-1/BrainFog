using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BlindSpire.Patches;

/// <summary>
/// Phobia mode toggling re-shows the creature body; re-apply the enemy mask (spec 0.01 4.1).
/// </summary>
[HarmonyPatch(typeof(NCreatureVisuals), "UpdatePhobiaMode")]
internal static class NCreatureVisualsPhobiaPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCreatureVisuals __instance)
    {
        if (ModRuntime.Disabled)
        {
            return;
        }

        var node = __instance.GetParent();
        while (node != null && node is not NCreature)
        {
            node = node.GetParent();
        }
        if (node is NCreature creature)
        {
            Game.EnemyVisualMask.Apply(creature);
        }
    }
}
