using System;
using System.Collections.Generic;

namespace BrainFog.Core.Reveal;

/// <summary>
/// Aligns persisted per-instance ids to the loaded deck after a save/load.
/// Index-only binding breaks when cards were added or removed since the order
/// was written, so ids are aligned by definition key first (tolerating
/// insertions/removals) and fall back to index alignment for legacy saves that
/// have no keys. Duplicated or invalid ids are dropped (fail closed).
/// </summary>
public static class DeckRebinder
{
    public readonly record struct Binding(int DeckIndex, string InstanceId);

    public static IReadOnlyList<Binding> Align(
        IReadOnlyList<string> deckKeys,
        IReadOnlyList<string> savedIds,
        IReadOnlyList<string>? savedKeys)
    {
        var result = new List<Binding>();
        if (savedIds.Count == 0 || deckKeys.Count == 0)
        {
            return result;
        }

        var untrusted = DuplicatedIds(savedIds);

        if (savedKeys == null || savedKeys.Count != savedIds.Count)
        {
            // Legacy save (0.2.2 and earlier): index-aligned prefix only.
            var legacyCount = Math.Min(deckKeys.Count, savedIds.Count);
            for (var i = 0; i < legacyCount; i++)
            {
                if (IsUsable(savedIds[i], untrusted))
                {
                    result.Add(new Binding(i, savedIds[i]));
                }
            }
            return result;
        }

        var deck = 0;
        var saved = 0;
        while (deck < deckKeys.Count && saved < savedKeys.Count)
        {
            if (string.Equals(deckKeys[deck], savedKeys[saved], StringComparison.Ordinal))
            {
                if (IsUsable(savedIds[saved], untrusted))
                {
                    result.Add(new Binding(deck, savedIds[saved]));
                }
                deck++;
                saved++;
                continue;
            }

            // Resync: a card was removed from the deck (saved slot unmatched) or
            // added to the deck (deck slot unmatched). Prefer the nearer match.
            var savedAhead = IndexOf(savedKeys, saved + 1, deckKeys[deck]);
            var deckAhead = IndexOf(deckKeys, deck + 1, savedKeys[saved]);
            if (savedAhead < 0 && deckAhead < 0)
            {
                deck++;
                saved++;
            }
            else if (deckAhead < 0 || (savedAhead >= 0 && savedAhead - saved <= deckAhead - deck))
            {
                saved = savedAhead; // skip saved slots until the deck key matches
            }
            else
            {
                deck++; // new deck card: skip it
            }
        }

        return result;
    }

    private static HashSet<string> DuplicatedIds(IReadOnlyList<string> ids)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var duplicated = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            if (!seen.Add(id))
            {
                duplicated.Add(id);
            }
        }
        return duplicated;
    }

    private static bool IsUsable(string id, HashSet<string> untrusted) =>
        InstanceIds.IsValid(id) && !untrusted.Contains(id);

    private static int IndexOf(IReadOnlyList<string> list, int from, string value)
    {
        for (var i = from; i < list.Count; i++)
        {
            if (string.Equals(list[i], value, StringComparison.Ordinal))
            {
                return i;
            }
        }
        return -1;
    }
}
