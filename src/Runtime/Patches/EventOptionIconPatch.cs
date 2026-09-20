using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace BrainFog.Patches;

/// <summary>
/// Event option icon visibility only (text blur moved to the unified source
/// patch 2026-09-21): ancient (Boss-relic) choices keep the relic icon so the
/// choice stays playable, Neow-style hidden choices hide it.
/// </summary>
[HarmonyPatch(typeof(NEventOptionButton))]
internal static class NEventOptionButtonIconPatch
{
    [HarmonyPatch("_Ready")]
    [HarmonyPostfix]
    private static void ReadyPostfix(NEventOptionButton __instance) =>
        Game.PatchGuard.Run("EventOptionIcon.Ready", () => EventChoiceIcons.ApplyTo(__instance));

    [HarmonyPatch("RefreshVotes")]
    [HarmonyPostfix]
    private static void RefreshVotesPostfix(NEventOptionButton __instance) =>
        Game.PatchGuard.Run("EventOptionIcon.Votes", () => EventChoiceIcons.ApplyTo(__instance));

    [HarmonyPatch("FlashConfirmation")]
    [HarmonyPostfix]
    private static void FlashConfirmationPostfix(NEventOptionButton __instance) =>
        Game.PatchGuard.Run("EventOptionIcon.Flash", () => EventChoiceIcons.ApplyTo(__instance));
}

internal static class EventChoiceIcons
{
    public static void ApplyTo(NEventOptionButton button)
    {
        if (ModRuntime.Disabled)
        {
            return;
        }

        if (Game.AncientChoiceRules.StaysVisible(button.Event))
        {
            return; // ancient options keep the relic icon visible
        }

        if (button.GetNodeOrNull<TextureRect>("%RelicIcon") is { } relicIcon)
        {
            relicIcon.Visible = false;
        }
    }
}
