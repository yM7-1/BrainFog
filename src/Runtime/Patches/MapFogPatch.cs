using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace BrainFog.Patches;

/// <summary>Map fog applied whenever the map opens or travelability changes.</summary>
[HarmonyPatch(typeof(NMapScreen))]
internal static class MapFogPatch
{
    [HarmonyPatch("Open")]
    [HarmonyPostfix]
    private static void OpenPostfix(NMapScreen __instance)
    {
        Game.MapFogController.Apply(__instance);
    }

    [HarmonyPatch("RecalculateTravelability")]
    [HarmonyPostfix]
    private static void RecalculatePostfix(NMapScreen __instance)
    {
        Game.MapFogController.Apply(__instance);
    }

    [HarmonyPatch("ProcessMouseDrawingEvent")]
    [HarmonyPrefix]
    private static bool ProcessMouseDrawingEventPrefix() =>
        Game.PatchGuard.RunOr("MapFog.DrawingPrefix", () => ModRuntime.Disabled, false);

    [HarmonyPatch("OnDrawingToolsHotkeyPressed")]
    [HarmonyPrefix]
    private static bool HotkeyPrefix() =>
        Game.PatchGuard.RunOr("MapFog.HotkeyPrefix", () => ModRuntime.Disabled, false);

    [HarmonyPatch("OnMapDrawingButtonPressed")]
    [HarmonyPrefix]
    private static bool DrawButtonPrefix() =>
        Game.PatchGuard.RunOr("MapFog.DrawButtonPrefix", () => ModRuntime.Disabled, false);

    [HarmonyPatch("OnMapErasingButtonPressed")]
    [HarmonyPrefix]
    private static bool EraseButtonPrefix() =>
        Game.PatchGuard.RunOr("MapFog.EraseButtonPrefix", () => ModRuntime.Disabled, false);
}
