using BrainFog.Game;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace BrainFog.Patches;

/// <summary>
/// Detects the run session at lobby creation. Multiplayer no longer disables
/// the mod (plan A, 2026-11-14); it only restricts knowledge/removal to the
/// local player. The mode is cached for Blitz/Dread UI adaptations.
/// </summary>
[HarmonyPatch(typeof(RunManager), "InitializeRunLobby")]
internal static class RunManagerMultiplayerGuardPatch
{
    [HarmonyPostfix]
    private static void Postfix(INetGameService netService)
    {
        ModRuntime.EvaluateMultiplayer(netService.Type.IsMultiplayer());
        RunModeContext.Set(RunManager.Instance.DebugOnlyGetState());
    }
}
