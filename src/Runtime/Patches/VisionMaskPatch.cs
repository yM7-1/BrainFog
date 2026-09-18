using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace BlindSpire.Patches;

/// <summary>Attaches the combat vision mask and re-evaluates the low-HP warning
/// on combat entry (spec 0.01 4.1, 0.03 d).</summary>
[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
internal static class VisionMaskPatch
{
    private const string MaskName = "BlindSpireVisionMaskLayer";

    [HarmonyPostfix]
    private static void Postfix(NCombatRoom __instance)
    {
        if (ModRuntime.Disabled)
        {
            return;
        }

        if (__instance.GetNodeOrNull<Game.VisionMask>(MaskName) == null)
        {
            __instance.AddChild(new Game.VisionMask { Name = MaskName });
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
