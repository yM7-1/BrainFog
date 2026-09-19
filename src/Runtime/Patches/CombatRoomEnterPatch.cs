using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace BrainFog.Patches;

/// <summary>Re-evaluates the low-HP warning on combat entry (spec 0.01 4.1, 0.03 d).
/// The former combat vision mask was removed after in-game feedback
/// (2026-09-19): only enemies stay masked, the rest of the combat scene is visible.</summary>
[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
internal static class CombatRoomEnterPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCombatRoom __instance)
    {
        if (ModRuntime.Disabled)
        {
            return;
        }

        foreach (var node in __instance.CreatureNodes)
        {
            if (node.Entity is { IsPlayer: true } && LocalContext.IsMe(node.Entity))
            {
                if (node.Entity.Player is { } player)
                {
                    Game.LowHpHintDisplay.Evaluate(player, node.Entity.CurrentHp, node.Entity.MaxHp);
                }
                break;
            }
        }
    }
}
