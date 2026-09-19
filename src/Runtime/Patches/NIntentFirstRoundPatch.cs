using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace BrainFog.Patches;

/// <summary>
/// Enemy intents only on the first round of the combat (spec 0.03 f);
/// summons that appear later never show intents.
/// </summary>
[HarmonyPatch(typeof(NIntent), "UpdateIntent")]
internal static class NIntentFirstRoundPatch
{
    [HarmonyPostfix]
    private static void Postfix(NIntent __instance, Creature owner)
    {
        if (ModRuntime.Disabled)
        {
            return;
        }

        var round = owner?.CombatState?.RoundNumber ?? 1;
        __instance.Visible = Game.IntentGate.ShouldShow(owner, round);
    }
}
