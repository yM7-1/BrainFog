using BrainFog.Core.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace BrainFog.Patches;

/// <summary>Merchant dialogue is garbled at 90% (user change 2026-09-19).
/// ShowRandom sets the raw line on every show, so blurring in the postfix is
/// idempotent and never double-applies.</summary>
[HarmonyPatch(typeof(NMerchantDialogue), "ShowRandom")]
internal static class MerchantDialogueBlurPatch
{
    [HarmonyPostfix]
    private static void Postfix(NMerchantDialogue __instance)
    {
        try
        {
            if (ModRuntime.Disabled)
            {
                return;
            }
            Game.TextBlurService.BlurNode(__instance._label, Game.DifficultyRuntime.TextBlurPercent);
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("MerchantDialogue.Blur", () => throw ex);
        }
    }
}
