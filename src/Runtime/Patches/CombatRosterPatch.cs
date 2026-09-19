using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace BrainFog.Patches;

/// <summary>Marks the opening combat roster (intent gate only allows these).</summary>
[HarmonyPatch(typeof(NCombatRoom), "OnCombatSetUp")]
internal static class CombatRosterPatch
{
    [HarmonyPostfix]
    private static void Postfix(CombatState state) =>
        Game.PatchGuard.Run("Roster.Mark", () =>
        {
            if (!ModRuntime.Disabled && state != null)
            {
                Game.IntentGate.MarkInitialCombatants(state.Creatures);
            }
        });
}
