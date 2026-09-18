using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace BlindSpire.Game;

/// <summary>
/// Enemy intents are shown only on round 1 of a combat (spec 0.03 f).
/// Once a combat passes round 1 (including phase transitions that may reset the
/// round counter), the gate stays locked for that creature.
/// </summary>
internal static class IntentGate
{
    private sealed class Flag
    {
        public bool Locked;
    }

    private static readonly ConditionalWeakTable<Creature, Flag> Map = new();

    public static bool ShouldShow(Creature? owner, int roundNumber)
    {
        if (owner == null)
        {
            return false;
        }

        var flag = Map.GetOrCreateValue(owner);
        if (roundNumber > 1)
        {
            flag.Locked = true;
        }
        return roundNumber == 1 && !flag.Locked;
    }
}
