using BrainFog.Core.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.addons.mega_text;

namespace BrainFog.Patches;

/// <summary>Map legend labels (enemy/merchant/treasure/rest/elite/unknown) are
/// garbled at 70% (user change 2026-09-19).</summary>
[HarmonyPatch(typeof(NMapLegendItem), "SetLocalizedFields")]
internal static class MapLegendTextBlurPatch
{
    [HarmonyPostfix]
    private static void Postfix(NMapLegendItem __instance)
    {
        try
        {
            if (ModRuntime.Disabled)
            {
                return;
            }
            var label = __instance.GetNodeOrNull<MegaLabel>("MegaLabel");
            Game.TextBlurService.BlurNode(label, TextBlurPercents.UiDescription);
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("MapLegend.Blur", () => throw ex);
        }
    }
}
