using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace BlindSpire.Patches;

/// <summary>Potions show only the outline (spec 0.01 4.3 / 0.02 #9).</summary>
[HarmonyPatch(typeof(NPotion), "Reload")]
internal static class PotionOutlinePatch
{
    [HarmonyPostfix]
    private static void Postfix(NPotion __instance)
    {
        if (ModRuntime.Disabled)
        {
            return;
        }
        if (__instance.Image != null && GodotObject.IsInstanceValid(__instance.Image))
        {
            __instance.Image.Visible = false;
        }
    }
}
