using System.Runtime.CompilerServices;
using BrainFog.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace BrainFog.Game;

/// <summary>
/// Per-creature intent gates (spec 0.03 f); semantics live in IntentRevealGate.
/// </summary>
internal static class IntentGate
{
    private sealed class Entry
    {
        public IntentRevealGate Gate { get; } = new();
        public bool IsInitialCombatant;
    }

    private static readonly ConditionalWeakTable<Creature, Entry> Map = new();

    /// <summary>Called at combat setup: only the opening roster may show intents.</summary>
    public static void MarkInitialCombatants(IEnumerable<Creature> creatures)
    {
        foreach (var creature in creatures)
        {
            if (creature != null)
            {
                Map.GetOrCreateValue(creature).IsInitialCombatant = true;
            }
        }
    }

    public static bool ShouldShow(Creature? owner, int roundNumber)
    {
        if (owner == null)
        {
            return false;
        }
        var entry = Map.GetOrCreateValue(owner);
        return entry.Gate.ShouldShow(roundNumber, entry.IsInitialCombatant);
    }
}
