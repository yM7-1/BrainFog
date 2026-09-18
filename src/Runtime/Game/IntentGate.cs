using System.Runtime.CompilerServices;
using BlindSpire.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace BlindSpire.Game;

/// <summary>
/// Per-creature intent gates (spec 0.03 f); semantics live in IntentRevealGate.
/// </summary>
internal static class IntentGate
{
    private static readonly ConditionalWeakTable<Creature, IntentRevealGate> Map = new();

    public static bool ShouldShow(Creature? owner, int roundNumber)
    {
        if (owner == null)
        {
            return false;
        }
        return Map.GetOrCreateValue(owner).ShouldShow(roundNumber);
    }
}
