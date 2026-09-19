using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using MegaCrit.Sts2.Core.Rewards;

namespace BrainFog.Patches;

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
            // Owned-relic display enabled: keep the real hover tip.
            if (!ModRuntime.Disabled && !Game.DifficultyRuntime.Current.ShowOwnedRelics)
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

/// <summary>Treasure-room (chest) relic choices hover-tip name+description; suppress
/// while masked (leak fix 2026-09-19).</summary>
[HarmonyPatch(typeof(NTreasureRoomRelicHolder), "OnFocus")]
internal static class TreasureRoomRelicHoverTipPatch
{
    [HarmonyPostfix]
    private static void Postfix(NTreasureRoomRelicHolder __instance) =>
        Game.PatchGuard.Run("RelicTips.Treasure", () =>
        {
            if (!ModRuntime.Disabled)
            {
                NHoverTipSet.Remove(__instance);
            }
        });
}

/// <summary>Shop relics hover-tip the real relic; suppress while masked
/// (leak fix 2026-09-19).</summary>
[HarmonyPatch(typeof(NMerchantRelic), "CreateHoverTip")]
internal static class MerchantRelicHoverTipPatch
{
    [HarmonyPostfix]
    private static void Postfix(NMerchantRelic __instance) =>
        Game.PatchGuard.Run("RelicTips.Merchant", () =>
        {
            if (!ModRuntime.Disabled)
            {
                NHoverTipSet.Remove(__instance);
            }
        });
}
