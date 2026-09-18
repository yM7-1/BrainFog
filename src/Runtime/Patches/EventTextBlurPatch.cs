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
        var input = title;
        title = Game.PatchGuard.RunOr(
            "EventBlur.Title",
            () => ModRuntime.Disabled ? input : EventTextBlurrer.Blur(input),
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
            () => ModRuntime.Disabled ? input : EventTextBlurrer.Blur(input),
            input);
    }
}

/// <summary>Event option buttons are blurred as well (spec 0.03 e: 全都模糊).
/// Boss relic choices (Ancient events) stay readable (spec 0.03 i).</summary>
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
    private const string OriginalMeta = "BlindSpireOriginalText";

    public static void ApplyTo(NEventOptionButton button)
    {
        if (ModRuntime.Disabled || button._label is not { } label)
        {
            return;
        }
        if (Game.AncientChoiceRules.StaysVisible(button.Event))
        {
            return;
        }

        // Neow-style hidden choices also hide the relic icon texture.
        if (button.GetNodeOrNull<Godot.TextureRect>("%RelicIcon") is { } relicIcon)
        {
            relicIcon.Visible = false;
        }

        var original = label.HasMeta(OriginalMeta)
            ? label.GetMeta(OriginalMeta).AsString()
            : label.Text;
        if (!label.HasMeta(OriginalMeta))
        {
            label.SetMeta(OriginalMeta, original);
        }

        var blurred = EventTextBlurrer.Blur(original);
        if (!string.Equals(label.Text, blurred, StringComparison.Ordinal))
        {
            label.Text = blurred;
            label.SetTextAutoSize(blurred);
        }
    }
}
