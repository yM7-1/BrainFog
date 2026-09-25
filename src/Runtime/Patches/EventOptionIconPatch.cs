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
    /// <summary>Marks a relic icon this mod hid (so the mod-off sweep can
    /// restore exactly the icons the game showed).</summary>
    public const string HiddenMeta = "BrainFogRelicIconHidden";

    public static void ApplyTo(NEventOptionButton button)
    {
        if (!GodotObject.IsInstanceValid(button))
        {
            return;
        }
        if (button.GetNodeOrNull<TextureRect>("%RelicIcon") is not { } relicIcon)
        {
            return;
        }

        if (ModRuntime.Disabled)
        {
            Restore(relicIcon);
            return;
        }

        if (Game.AncientChoiceRules.StaysVisible(button.Event))
        {
            return; // ancient options keep the relic icon visible
        }

        if (relicIcon.Visible)
        {
            relicIcon.SetMeta(HiddenMeta, true);
            relicIcon.Visible = false;
        }
    }

    private static void Restore(TextureRect relicIcon)
    {
        if (relicIcon.HasMeta(HiddenMeta))
        {
            relicIcon.Visible = true;
            relicIcon.RemoveMeta(HiddenMeta);
        }
    }
}
