using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Rewards;

namespace BlindSpire.Patches;

/// <summary>
/// Relics are invisible (spec 0.01 3.1): focus hover tips must not reveal their
/// name/description. Boss relic choices (Ancient event buttons) are unaffected.
/// </summary>
[HarmonyPatch(typeof(NRelicBasicHolder), "OnFocus")]
internal static class RelicBasicHolderHoverTipPatch
{
    [HarmonyPostfix]
    private static void Postfix(NRelicBasicHolder __instance) =>
        Game.PatchGuard.Run("RelicTips.Basic", () =>
        {
            if (!ModRuntime.Disabled)
            {
                NHoverTipSet.Remove(__instance);
            }
        });
}

[HarmonyPatch(typeof(NRelicInventoryHolder), "OnFocus")]
internal static class RelicInventoryHolderHoverTipPatch
{
    [HarmonyPostfix]
    private static void Postfix(NRelicInventoryHolder __instance) =>
        Game.PatchGuard.Run("RelicTips.Inventory", () =>
        {
            if (!ModRuntime.Disabled)
            {
                NHoverTipSet.Remove(__instance);
            }
        });
}

/// <summary>Relic reward rows hover-tip the real relic; suppress while masked.</summary>
[HarmonyPatch(typeof(RelicReward), "ExtraHoverTips", MethodType.Getter)]
internal static class RelicRewardHoverTipPatch
{
    [HarmonyPrefix]
    private static bool Prefix(ref IEnumerable<IHoverTip> __result)
    {
        if (ModRuntime.Disabled)
        {
            return true;
        }
        __result = Array.Empty<IHoverTip>();
        return false;
    }
}
