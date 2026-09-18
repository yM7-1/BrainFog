using BlindSpire.Core.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace BlindSpire.Patches;

/// <summary>
/// Event text is blurred at 75%, fixed per text (spec 0.03 e).
/// Prefixes rewrite the incoming string so repeated renders stay stable.
/// </summary>
[HarmonyPatch(typeof(NEventLayout))]
internal static class NEventLayoutBlurPatch
{
    [HarmonyPatch("SetTitle")]
    [HarmonyPrefix]
    private static void SetTitlePrefix(ref string title)
    {
        if (!ModRuntime.Disabled)
        {
            title = EventTextBlurrer.Blur(title);
        }
    }

    [HarmonyPatch("SetDescription")]
    [HarmonyPrefix]
    private static void SetDescriptionPrefix(ref string description)
    {
        if (!ModRuntime.Disabled)
        {
            description = EventTextBlurrer.Blur(description);
        }
    }
}

/// <summary>Event option buttons are blurred as well (spec 0.03 e: 全都模糊).
/// Boss relic choices (Ancient events) stay readable (spec 0.03 i).</summary>
[HarmonyPatch(typeof(NEventOptionButton), "_Ready")]
internal static class NEventOptionButtonBlurPatch
{
    [HarmonyPostfix]
    private static void Postfix(NEventOptionButton __instance)
    {
        if (ModRuntime.Disabled || __instance._label is not { } label)
        {
            return;
        }
        if (__instance.Event is MegaCrit.Sts2.Core.Models.AncientEventModel)
        {
            return;
        }
        label.Text = EventTextBlurrer.Blur(label.Text);
    }
}
