using BrainFog.Core.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace BrainFog.Patches;

/// <summary>
/// Event text is blurred at 75%, fixed per text within a launch and re-rolled
/// per launch (user change 2026-09-20). Prefixes rewrite the incoming string so
/// repeated renders stay stable.
/// </summary>
[HarmonyPatch(typeof(NEventLayout))]
internal static class NEventLayoutBlurPatch
{
    [HarmonyPatch("SetTitle")]
    [HarmonyPrefix]
    private static void SetTitlePrefix(ref string title)
    {
        var input = title;
        title = Game.PatchGuard.RunOr(
            "EventBlur.Title",
            () => ModRuntime.Disabled ? input : EventTextBlurrer.Blur(input, EventTextBlurrer.BlurPercent, BlurSalt.Current),
            input);
    }

    [HarmonyPatch("SetTitle")]
    [HarmonyPostfix]
    private static void SetTitlePostfix(NEventLayout __instance)
    {
        Game.PatchGuard.Run("EventBlur.TitleResize", () =>
        {
            if (!ModRuntime.Disabled && __instance._title is { } label)
            {
                label.SetTextAutoSize(label.Text);
                Game.TextBlurService.Mark(label);
            }
        });
    }

    [HarmonyPatch("SetDescription")]
    [HarmonyPrefix]
    private static void SetDescriptionPrefix(ref string description)
    {
        var input = description;
        description = Game.PatchGuard.RunOr(
            "EventBlur.Description",
            () => ModRuntime.Disabled ? input : EventTextBlurrer.Blur(input, EventTextBlurrer.BlurPercent, BlurSalt.Current),
            input);
    }

    [HarmonyPatch("SetDescription")]
    [HarmonyPostfix]
    private static void SetDescriptionPostfix(NEventLayout __instance) =>
        Game.PatchGuard.Run("EventBlur.DescriptionMark", () =>
        {
            if (!ModRuntime.Disabled)
            {
                Game.TextBlurService.Mark(__instance._description);
            }
        });
}

/// <summary>Event option buttons are blurred as well (spec 0.03 e: 全都模糊).
/// Ancient options are garbled at 90% but keep the relic icon visible so the
/// Boss-relic choice stays playable (user change 2026-09-19). Neow-style hidden
/// choices also hide the icon.</summary>
[HarmonyPatch(typeof(NEventOptionButton))]
internal static class NEventOptionButtonBlurPatch
{
    [HarmonyPatch("_Ready")]
    [HarmonyPostfix]
    private static void ReadyPostfix(NEventOptionButton __instance) =>
        Game.PatchGuard.Run("EventBlur.OptionReady", () => EventTextBlurHelper.ApplyTo(__instance));

    [HarmonyPatch("RefreshVotes")]
    [HarmonyPostfix]
    private static void RefreshVotesPostfix(NEventOptionButton __instance) =>
        Game.PatchGuard.Run("EventBlur.OptionVotes", () => EventTextBlurHelper.ApplyTo(__instance));

    [HarmonyPatch("FlashConfirmation")]
    [HarmonyPostfix]
    private static void FlashConfirmationPostfix(NEventOptionButton __instance) =>
        Game.PatchGuard.Run("EventBlur.OptionFlash", () => EventTextBlurHelper.ApplyTo(__instance));
}

/// <summary>Idempotent option-text blurring: remembers the original text so
/// re-renders stay garbled but never double-blur.</summary>
internal static class EventTextBlurHelper
{

    public static void ApplyTo(NEventOptionButton button)
    {
        if (ModRuntime.Disabled || button._label is not { } label)
        {
            return;
        }

        if (Game.AncientChoiceRules.StaysVisible(button.Event))
        {
            // Ancient dialogue options: 90% garbled, icon stays visible.
            ApplyBlur(label, TextBlurPercents.Dialogue);
            return;
        }

        // Neow-style hidden choices also hide the relic icon texture.
        if (button.GetNodeOrNull<Godot.TextureRect>("%RelicIcon") is { } relicIcon)
        {
            relicIcon.Visible = false;
        }

        ApplyBlur(label, TextBlurPercents.Event);
    }

    private static void ApplyBlur(MegaCrit.Sts2.addons.mega_text.MegaRichTextLabel label, int percent) =>
        Game.TextBlurService.BlurNode(label, percent);
}
