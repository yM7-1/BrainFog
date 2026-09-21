using System;
using System.Collections.Generic;
using BrainFog.Core.Options;
using BrainFog.Core.Reveal;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BrainFog.Game;

/// <summary>
/// "Play counter" (user change 2026-09-21): counts every play per card copy for
/// the run and feeds the top-right leaderboard. Copies of the same definition
/// get stable numbers in deck order ("打击1", "打击2"); numbers are always
/// assigned so toggling the feature on later keeps them, but counts are only
/// recorded while the feature is on. Deck additions also start revealed in
/// bad-memory mode.
/// </summary>
internal static class PlayCounterTracker
{
    public readonly record struct Row(string Label, int Count);

    private static readonly Dictionary<string, int> Counts = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> Numbers = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> Names = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> NextNumbers = new(StringComparer.Ordinal);
    private static WeakReference<RunState>? _runRef;

    /// <summary>Leaderboard content changed (raised on the main thread).</summary>
    public static event Action? Changed;

    public static bool Enabled => !ModRuntime.Disabled && DifficultyRuntime.Current.PlayCounter;

    /// <summary>Run start/load: replaces the in-memory counter state.</summary>
    public static void OnRunLoaded(
        IReadOnlyDictionary<string, int>? counts,
        IReadOnlyDictionary<string, int>? numbers,
        IReadOnlyDictionary<string, string>? names,
        IReadOnlyDictionary<string, int>? nextNumbers) =>
        PatchGuard.Run("Counter.Load", () =>
        {
            Counts.Clear();
            Numbers.Clear();
            Names.Clear();
            NextNumbers.Clear();
            CopyInto(counts, Counts);
            CopyInto(numbers, Numbers);
            CopyInto(names, Names);
            CopyInto(nextNumbers, NextNumbers);
            Changed?.Invoke();
        });

    /// <summary>Assigns numbers to every deck copy that has none yet.</summary>
    public static void EnsureDeckNumbers(RunState state) =>
        PatchGuard.Run("Counter.EnsureNumbers", () =>
        {
            _runRef = new WeakReference<RunState>(state);
            foreach (var player in state.Players)
            {
                foreach (var card in player.Deck.Cards)
                {
                    AssignNumber(card);
                }
            }
            Changed?.Invoke();
        });

    /// <summary>Option toggled: make sure numbers exist, refresh the board.</summary>
    public static void OnSettingChanged() =>
        PatchGuard.Run("Counter.SettingChanged", () =>
        {
            if (_runRef != null && _runRef.TryGetTarget(out var state))
            {
                EnsureDeckNumbers(state);
            }
            Changed?.Invoke();
        });

    /// <summary>Deck addition: number the copy; bad memory also reveals it.</summary>
    public static void OnCardAddedToDeck(CardModel card) =>
        PatchGuard.Run("Counter.DeckAdded", () =>
        {
            if (ModRuntime.Disabled)
            {
                return;
            }

            if (DifficultyRuntime.Current.MemoryMode == CardMemoryMode.BadMemory)
            {
                RevealNewCopy(card);
            }
            AssignNumber(card);
            Changed?.Invoke();
        });

    /// <summary>Play hook: counts the play of a run-deck copy.</summary>
    public static void OnPlayed(CardModel card) =>
        PatchGuard.Run("Counter.Played", () =>
        {
            if (!Enabled)
            {
                return;
            }

            var ownerCard = CardInstanceRegistry.ResolveIdentity(card);
            if (ownerCard.Pile?.Type != PileType.Deck)
            {
                return; // tokens generated in combat have no deck slot
            }

            var id = CardInstanceRegistry.TryGetId(ownerCard);
            if (string.IsNullOrEmpty(id))
            {
                AssignNumber(ownerCard);
                id = CardInstanceRegistry.TryGetId(ownerCard);
            }
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            Counts[id] = Counts.TryGetValue(id, out var count) ? count + 1 : 1;
            Names[id] = BaseTitle(ownerCard);
            RevealPersistence.SetPlayCount(id, Counts[id]);
            Changed?.Invoke();
        });

    /// <summary>Copies left the deck (memory fade): drop their board entries.</summary>
    public static void OnCardsRemoved(IReadOnlyCollection<string> instanceIds) =>
        PatchGuard.Run("Counter.Removed", () =>
        {
            var changed = false;
            foreach (var id in instanceIds)
            {
                changed |= Counts.Remove(id);
                Numbers.Remove(id);
                Names.Remove(id);
            }
            if (changed)
            {
                Changed?.Invoke();
            }
        });

    /// <summary>Rows for the leaderboard, play count descending.</summary>
    public static IReadOnlyList<Row> GetRows()
    {
        var live = BuildLiveNames();
        var rows = new List<Row>(Counts.Count);
        foreach (var (id, count) in Counts)
        {
            if (count <= 0)
            {
                continue;
            }
            var name = live.TryGetValue(id, out var title)
                ? title
                : Names.TryGetValue(id, out var stored) ? stored : "?";
            var number = Numbers.TryGetValue(id, out var assigned) ? assigned : 0;
            rows.Add(new Row(CardNumberAllocator.FormatLabel(name, number), count));
        }
        rows.Sort((a, b) => a.Count != b.Count
            ? b.Count.CompareTo(a.Count)
            : string.CompareOrdinal(a.Label, b.Label));
        return rows;
    }

    private static Dictionary<string, string> BuildLiveNames()
    {
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        if (_runRef != null && _runRef.TryGetTarget(out var state))
        {
            foreach (var player in state.Players)
            {
                foreach (var card in player.Deck.Cards)
                {
                    var id = CardInstanceRegistry.TryGetId(card);
                    if (!string.IsNullOrEmpty(id))
                    {
                        names[id] = BaseTitle(card);
                    }
                }
            }
        }
        return names;
    }

    private static void RevealNewCopy(CardModel card)
    {
        var id = CardInstanceRegistry.GetOrCreateId(card);
        if (string.IsNullOrEmpty(id) || !ModRuntime.Tracker.RevealInstanceByPlay(id))
        {
            return;
        }
        RevealPersistence.OnInstanceRevealed(card, id);
        Callable.From(() => CardFogRenderer.RefreshLiveCards(card)).CallDeferred();
    }

    private static void AssignNumber(CardModel card)
    {
        var id = CardInstanceRegistry.GetOrCreateId(card);
        if (string.IsNullOrEmpty(id) || Numbers.ContainsKey(id))
        {
            return;
        }

        var key = RevealKeys.Of(card);
        var number = CardNumberAllocator.NextFor(key, NextNumbers);
        Numbers[id] = number;
        Names[id] = BaseTitle(card);
        RevealPersistence.SetCardNumber(id, number, Names[id]);
        RevealPersistence.SetNextCardNumber(key, NextNumbers[key]);
    }

    /// <summary>Base (non-upgraded) title, so "打击+" and "打击" share a name.</summary>
    private static string BaseTitle(CardModel card)
    {
        try
        {
            return card.TitleLocString.GetFormattedText();
        }
        catch
        {
            return card.Title;
        }
    }

    private static void CopyInto<TValue>(
        IReadOnlyDictionary<string, TValue>? source,
        Dictionary<string, TValue> target)
    {
        if (source == null)
        {
            return;
        }
        foreach (var (key, value) in source)
        {
            if (!string.IsNullOrEmpty(key))
            {
                target[key] = value;
            }
        }
    }
}
