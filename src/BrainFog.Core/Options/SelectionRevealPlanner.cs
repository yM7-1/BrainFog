using System;
using System.Collections.Generic;
using System.Linq;

namespace BrainFog.Core.Options;

/// <summary>
/// Picks which offered cards in a reward selection screen are revealed when the
/// difficulty option is "random 1/2/3". The pick is deterministic per offer
/// (stable across refreshes of the same screen) and salted per game launch so
/// different runs see different slots.
/// </summary>
public static class SelectionRevealPlanner
{
    /// <summary>
    /// Returns the indexes to reveal among <paramref name="orderedKeys"/>
    /// (one key per offered card, in display order).
    /// </summary>
    public static IReadOnlyCollection<int> ResolveRevealedIndexes(
        IReadOnlyList<string> orderedKeys,
        SelectionRevealOption option,
        int salt)
    {
        var count = orderedKeys?.Count ?? 0;
        if (count <= 0 || option == SelectionRevealOption.None)
        {
            return Array.Empty<int>();
        }
        if (option == SelectionRevealOption.All)
        {
            return Enumerable.Range(0, count).ToArray();
        }

        var wanted = Math.Min((int)option, count);
        return Enumerable.Range(0, count)
            .Select(index => (index, score: Score(salt, index, orderedKeys![index])))
            .OrderBy(pair => pair.score)
            .ThenBy(pair => pair.index)
            .Take(wanted)
            .Select(pair => pair.index)
            .OrderBy(index => index)
            .ToArray();
    }

    public static bool IsRevealed(
        IReadOnlyList<string> orderedKeys,
        SelectionRevealOption option,
        int salt,
        int index)
    {
        if (index < 0)
        {
            return option == SelectionRevealOption.All && (orderedKeys?.Count ?? 0) > 0;
        }
        foreach (var revealed in ResolveRevealedIndexes(orderedKeys ?? Array.Empty<string>(), option, salt))
        {
            if (revealed == index)
            {
                return true;
            }
        }
        return false;
    }

    private static int Score(int salt, int index, string key)
    {
        unchecked
        {
            var hash = 2166136261u;
            var text = salt + ":" + index + ":" + key;
            foreach (var ch in text)
            {
                hash ^= ch;
                hash *= 16777619u;
            }
            return (int)hash;
        }
    }
}
