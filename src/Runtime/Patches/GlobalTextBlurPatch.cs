using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;

namespace BrainFog.Patches;

/// <summary>
/// Blurs general UI text at the source (2026-09-21): the game writes nearly all
/// UI strings through these two methods, so rewriting the incoming string in a
/// prefix replaces the old 0.2s scene sweep (which stays only as a fallback for
/// plain Label/RichTextLabel controls).
///
/// Runs after the card-face patch (Priority.LowerThanNormal): card labels are handled
/// there, and the output metadata lets this prefix skip them cheaply.
/// </summary>
[HarmonyPatch(typeof(MegaLabel), "SetTextAutoSize")]
[HarmonyPriority(Priority.LowerThanNormal)]
internal static class GlobalMegaLabelTextBlurPatch
{
    [HarmonyPrefix]
    private static void Prefix(MegaLabel __instance, ref string text) =>
        Game.GlobalTextBlurSource.Apply(__instance, ref text);
}

[HarmonyPatch(typeof(MegaRichTextLabel), "SetTextAutoSize")]
[HarmonyPriority(Priority.LowerThanNormal)]
internal static class GlobalMegaRichTextLabelBlurPatch
{
    [HarmonyPrefix]
    private static void Prefix(MegaRichTextLabel __instance, ref string text) =>
        Game.GlobalTextBlurSource.Apply(__instance, ref text);
}
