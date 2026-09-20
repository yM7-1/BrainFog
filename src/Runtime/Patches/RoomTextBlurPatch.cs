using BrainFog.Core.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace BrainFog.Patches;

/// <summary>Room-advance ("proceed") button and rest-site option texts are
/// garbled (user change 2026-09-19; no ratio specified, 60% default).</summary>
[HarmonyPatch(typeof(NProceedButton), "UpdateText")]
internal static class ProceedButtonTextBlurPatch
{
    [HarmonyPostfix]
    private static void Postfix(NProceedButton __instance)
    {
        try
        {
            if (!ModRuntime.Disabled)
            {
                Game.TextBlurService.BlurNode(__instance._label, Game.DifficultyRuntime.TextBlurPercent);
            }
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("ProceedBlur", () => throw ex);
        }
    }
}

[HarmonyPatch(typeof(NRestSiteButton), "Reload")]
internal static class RestSiteButtonTextBlurPatch
{
    [HarmonyPostfix]
    private static void Postfix(NRestSiteButton __instance)
    {
        try
        {
            if (!ModRuntime.Disabled)
            {
                Game.TextBlurService.BlurNode(__instance._label, Game.DifficultyRuntime.TextBlurPercent);
            }
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("RestSiteBlur", () => throw ex);
        }
    }
}
