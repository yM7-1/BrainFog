using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace BrainFog.Patches;

/// <summary>
/// Event title bug fix (2026-09-21): <c>NEventLayout.SetTitle</c> assigns
/// <c>_title.Text</c> directly, which bypasses <c>MegaLabel.SetTextAutoSize</c>
/// and therefore the source blur (the event name stayed readable). Re-routing
/// through SetTextAutoSize blurs it like every other text.
/// </summary>
[HarmonyPatch(typeof(NEventLayout), "SetTitle")]
internal static class EventTitleBlurPatch
{
    [HarmonyPostfix]
    private static void Postfix(NEventLayout __instance)
    {
        if (ModRuntime.Disabled || __instance._title is not { } title)
        {
            return;
        }
        Game.PatchGuard.Run("EventTitle.Route", () => title.SetTextAutoSize(title.Text));
    }
}
