using BrainFog.Core.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BrainFog.Patches;

/// <summary>The "battle start" banner text is garbled (user change 2026-09-19;
/// no ratio specified, so the 60% default applies).</summary>
[HarmonyPatch(typeof(NCombatStartBanner), "_Ready")]
internal static class CombatStartBannerBlurPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCombatStartBanner __instance)
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
            Game.PatchGuard.Run("CombatStartBlur", () => throw ex);
        }
    }
}
