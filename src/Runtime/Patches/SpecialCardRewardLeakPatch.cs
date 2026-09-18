using BlindSpire.Core.Reveal;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;

namespace BlindSpire.Patches;

/// <summary>
/// Special card rewards (thief return, quest cards) embed the real card title
/// and a card hover tip; hide both while the instance is unknown (leak audit).
/// </summary>
[HarmonyPatch(typeof(SpecialCardReward))]
internal static class SpecialCardRewardLeakPatch
{
    [HarmonyPatch("Description", MethodType.Getter)]
    [HarmonyPrefix]
    private static bool DescriptionPrefix(SpecialCardReward __instance, ref LocString __result)
    {
        try
        {
            if (!ShouldHide(__instance))
            {
                return true;
            }

            var loc = new LocString("gameplay_ui", "COMBAT_REWARD_ADD_SPECIAL_CARD");
            loc.Add("Card", Game.ModLocalization.UnknownCard);
            __result = loc;
            return false;
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("SpecialReward.Description", () => throw ex);
            return true;
        }
    }

    [HarmonyPatch("ExtraHoverTips", MethodType.Getter)]
    [HarmonyPrefix]
    private static bool ExtraHoverTipsPrefix(SpecialCardReward __instance, ref IEnumerable<IHoverTip> __result)
    {
        try
        {
            if (!ShouldHide(__instance))
            {
                return true;
            }

            __result = Array.Empty<IHoverTip>();
            return false;
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("SpecialReward.Tips", () => throw ex);
            return true;
        }
    }

    private static bool ShouldHide(SpecialCardReward reward)
    {
        if (ModRuntime.Disabled || reward._card is not { } card)
        {
            return false;
        }
        var id = Game.CardInstanceRegistry.GetOrCreateId(card);
        return ModRuntime.Tracker.GetKnowledge(id) == CardKnowledge.Unknown;
    }
}
