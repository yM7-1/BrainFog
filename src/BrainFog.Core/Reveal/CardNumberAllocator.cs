using System;
using System.Collections.Generic;

namespace BrainFog.Core.Reveal;

/// <summary>One copy's stable number ("打击1", "打击2").</summary>
public readonly record struct CardNumberAssignment(string Id, string DefinitionKey, int Number);

/// <summary>
/// Stable per-copy numbering for the "play counter" (user change 2026-09-21):
/// same-name copies are numbered in the order they joined the deck; a copy's
/// number never shifts when other copies leave the deck, and a new copy
/// continues after the highest number ever assigned for its definition.
/// </summary>
public static class CardNumberAllocator
{
    /// <summary>Assigns numbers to deck copies that have none yet.
    /// <paramref name="deckOrder"/> follows the deck order; the persisted
    /// per-definition "next number" counter keeps removed copies' numbers
    /// from being reused.</summary>
    public static List<CardNumberAssignment> AssignMissing(
        IReadOnlyList<(string Id, string DefinitionKey)> deckOrder,
        IReadOnlyDictionary<string, int> assigned,
        IDictionary<string, int> nextNumber)
    {
        var result = new List<CardNumberAssignment>();
        foreach (var (id, key) in deckOrder)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(key) || assigned.ContainsKey(id))
            {
                continue;
            }

            var number = NextFor(key, nextNumber);
            result.Add(new CardNumberAssignment(id, key, number));
        }
        return result;
    }

    /// <summary>Consumes and returns the next number for one copy.</summary>
    public static int NextFor(string definitionKey, IDictionary<string, int> nextNumber)
    {
        var next = nextNumber.TryGetValue(definitionKey, out var stored) && stored > 0 ? stored : 1;
        nextNumber[definitionKey] = next + 1;
        return next;
    }

    /// <summary>Board label ("打击2"); the number is omitted when unknown.</summary>
    public static string FormatLabel(string title, int number) =>
        number > 0 ? $"{title}{number}" : title;
}
