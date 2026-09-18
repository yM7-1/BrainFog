using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace BlindSpire.Patches;

/// <summary>
/// Keeps every live NCard in a Godot group so reveal refreshes are precise
/// (no full-scene scans). Cheap: one group add at node ready.
/// </summary>
[HarmonyPatch(typeof(NCard), "_Ready")]
internal static class NCardGroupPatch
{
    internal const string GroupName = "blindspire_cards";

    [HarmonyPostfix]
    private static void Postfix(NCard __instance) =>
        Game.PatchGuard.Run("NCard.GroupAdd", () =>
        {
            if (Godot.GodotObject.IsInstanceValid(__instance) && !__instance.IsInGroup(GroupName))
            {
                __instance.AddToGroup(GroupName);
            }
        });
}
