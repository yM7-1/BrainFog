using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace BlindSpire.Patches;

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
}
