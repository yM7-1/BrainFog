using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace BrainFog.Patches;

/// <summary>
/// Detects multiplayer runs and disables BrainFog for them (spec 0.02 #10).
/// </summary>
[HarmonyPatch(typeof(RunManager), "InitializeRunLobby")]
internal static class RunManagerMultiplayerGuardPatch
{
    [HarmonyPostfix]
    private static void Postfix(INetGameService netService)
    {
        ModRuntime.EvaluateMultiplayer(netService.Type.IsMultiplayer());
    }
}
