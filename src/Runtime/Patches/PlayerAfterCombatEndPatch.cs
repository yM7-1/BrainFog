using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;

namespace BrainFog.Patches;

/// <summary>
/// "Memory fade" teardown hook (user change 2026-09-21): Player.AfterCombatEnd
/// runs after the combat state is cleared but before the game writes the
/// post-combat save, so removals persist immediately.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.AfterCombatEnd))]
internal static class PlayerAfterCombatEndPatch
{
    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        if (!ModRuntime.Disabled && LocalContext.IsMe(__instance))
        {
            Game.MemoryFade.OnCombatEnded(__instance);
        }
    }
}
