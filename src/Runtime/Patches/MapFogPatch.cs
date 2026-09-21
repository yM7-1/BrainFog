using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace BrainFog.Patches;

/// <summary>
/// Map fog applied whenever the map opens or travelability changes.
/// The original map drawing tools (buttons, right-click drawing, hotkeys) are
/// untouched since 0.3.3 (user change 2026-09-21); the fog only hides nodes and
/// paths.
/// </summary>
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
}
