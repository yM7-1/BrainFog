using System;
using System.Collections.Generic;

namespace BrainFog.Core.Reveal;

/// <summary>
/// Index-aligned rebinding of persisted instance ids to deck slots after a load.
/// Fails closed on ambiguity (out-of-range index, invalid or duplicated ids) so a
/// drifted deck order never reveals the wrong card.
/// </summary>
public static class DeckOrderBinding
{
    public static bool TryResolve(int index, IReadOnlyList<string>? savedIds, out string id, out string? error)
    {
        id = string.Empty;
        error = null;

        if (savedIds == null || savedIds.Count == 0)
        {
            error = "no saved deck order";
            return false;
        }
        if (index < 0 || index >= savedIds.Count)
        {
            error = "deck index out of range";
            return false;
        }

        var candidate = savedIds[index];
        if (!InstanceIds.IsValid(candidate))
        {
            error = "invalid instance id";
            return false;
        }

        for (var i = 0; i < savedIds.Count; i++)
        {
            if (i != index && string.Equals(savedIds[i], candidate, StringComparison.Ordinal))
            {
                error = "duplicate instance id in saved order";
                return false;
            }
        }

        id = candidate;
        return true;
    }
}
