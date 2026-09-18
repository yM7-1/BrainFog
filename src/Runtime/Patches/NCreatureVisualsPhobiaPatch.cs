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
    private static void Postfix(NCreatureVisuals __instance) =>
        Game.PatchGuard.Run("CreatureVisuals.Phobia", () => CreatureVisualsReapply.Reapply(__instance));
}

/// <summary>Skin/form swaps re-show the body node; keep the mask on.</summary>
[HarmonyPatch(typeof(NCreatureVisuals), "SetUpSkin")]
internal static class NCreatureVisualsSkinPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCreatureVisuals __instance) =>
        Game.PatchGuard.Run("CreatureVisuals.Skin", () => CreatureVisualsReapply.Reapply(__instance));
}

internal static class CreatureVisualsReapply
{
    internal static void Reapply(NCreatureVisuals visuals)
    {
        if (ModRuntime.Disabled)
        {
            return;
        }

        var node = visuals.GetParent();
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
