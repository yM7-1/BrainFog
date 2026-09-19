using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BrainFog.Patches;

/// <summary>Enemy nameplates never show their name (user change 2026-09-19).
/// The HP bar and power glyphs stay readable on hover.</summary>
[HarmonyPatch(typeof(NCreatureStateDisplay), "SetCreature")]
internal static class EnemyNameHidePatch
{
    [HarmonyPostfix]
    private static void Postfix(NCreatureStateDisplay __instance, Creature creature)
    {
        try
        {
            if (ModRuntime.Disabled || !creature.IsEnemy)
            {
                return;
            }

            __instance._nameplateLabel.Visible = false;
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("EnemyName.Hide", () => throw ex);
        }
    }
}
