using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace BlindSpire.Patches;

/// <summary>Attaches the combat vision mask to each combat room (spec 0.01 4.1).</summary>
[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
internal static class VisionMaskPatch
{
    private const string MaskName = "BlindSpireVisionMaskLayer";

    [HarmonyPostfix]
    private static void Postfix(NCombatRoom __instance)
    {
        if (ModRuntime.Disabled || __instance.GetNodeOrNull<Game.VisionMask>(MaskName) != null)
        {
            return;
        }

        var mask = new Game.VisionMask { Name = MaskName };
        __instance.AddChild(mask);
    }
}
