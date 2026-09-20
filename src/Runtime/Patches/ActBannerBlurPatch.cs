using BrainFog.Core.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace BrainFog.Patches;

/// <summary>The act-transition banner ("Act 3 - Glory") is garbled
/// (user change 2026-09-19; no ratio specified, 60% default).</summary>
[HarmonyPatch(typeof(NActBanner), "_Ready")]
internal static class ActBannerBlurPatch
{
    [HarmonyPostfix]
    private static void Postfix(NActBanner __instance)
    {
        try
        {
            if (ModRuntime.Disabled)
            {
                return;
            }
            Game.TextBlurService.BlurNode(__instance._actNumber, Game.DifficultyRuntime.TextBlurPercent);
            Game.TextBlurService.BlurNode(__instance._actName, Game.DifficultyRuntime.TextBlurPercent);
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("ActBanner.Blur", () => throw ex);
        }
    }
}
