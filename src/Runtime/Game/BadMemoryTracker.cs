using System.Runtime.CompilerServices;
using BrainFog.Core.Options;
using BrainFog.Core.Reveal;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace BrainFog.Game;

/// <summary>
/// "Bad memory" mode (user change 2026-09-21): every card copy counts hand
/// entries that end without a play; when the count reaches n the copy reverts to
/// unknown. Playing resets the counter. Counters persist with the run, keyed by
/// the BrainFog instance id (rebound from the deck order on load).
/// </summary>
internal static class BadMemoryTracker
{
    private sealed class Entry
    {
        public readonly BadMemoryCounter Counter = new();
        public bool Seeded;
    }

    private static readonly ConditionalWeakTable<CardModel, Entry> Map = new();
    private static Dictionary<string, int> _savedCounters = new(StringComparer.Ordinal);

    /// <summary>Run load: restores persisted counters (called after deck binding).</summary>
    public static void OnRunLoaded(IReadOnlyDictionary<string, int>? counters)
    {
        _savedCounters = counters == null
            ? new Dictionary<string, int>(StringComparer.Ordinal)
            : counters.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    public static void OnEnterHand(CardModel card) =>
        PatchGuard.Run("BadMemory.Enter", () =>
        {
            if (!Active)
            {
                return;
            }
            GetEntry(card).Counter.OnEnterHand();
        });

    public static void OnPlayed(CardModel card) =>
        PatchGuard.Run("BadMemory.Played", () =>
        {
            if (!Active)
            {
                return;
            }
            var entry = GetEntry(card);
            entry.Counter.OnPlayed();
            Persist(card, entry, 0);
        });

    public static void OnLeaveHand(CardModel card) =>
        PatchGuard.Run("BadMemory.Leave", () =>
        {
            if (!Active)
            {
                return;
            }
            var entry = GetEntry(card);
            var threshold = DifficultySettings.ClampBadMemoryThreshold(
                DifficultyRuntime.Current.BadMemoryThreshold);
            if (entry.Counter.OnLeaveHand(threshold))
            {
                Hide(card);
                Persist(card, entry, 0);
                return;
            }
            Persist(card, entry, entry.Counter.UnplayedDraws);
        });

    private static bool Active =>
        !ModRuntime.Disabled && DifficultyRuntime.Current.MemoryMode == CardMemoryMode.BadMemory;

    private static Entry GetEntry(CardModel card)
    {
        var owner = CardInstanceRegistry.ResolveIdentity(card);
        var entry = Map.GetOrCreateValue(owner);
        if (!entry.Seeded)
        {
            entry.Seeded = true;
            var id = CardInstanceRegistry.TryGetId(owner);
            if (!string.IsNullOrEmpty(id) && _savedCounters.TryGetValue(id, out var saved))
            {
                entry.Counter.Restore(saved);
            }
        }
        return entry;
    }

    private static void Persist(CardModel card, Entry entry, int count)
    {
        var id = CardInstanceRegistry.GetOrCreateId(card);
        if (!string.IsNullOrEmpty(id))
        {
            RevealPersistence.SetBadMemoryCounter(id, count);
        }
    }

    /// <summary>Reverts the copy to unknown and refreshes its live visuals.</summary>
    private static void Hide(CardModel card)
    {
        var id = CardInstanceRegistry.GetOrCreateId(card);
        if (string.IsNullOrEmpty(id))
        {
            return;
        }
        if (ModRuntime.Tracker.HideInstance(id))
        {
            RevealPersistence.OnInstanceHidden(id);
            Callable.From(() => CardFogRenderer.RefreshLiveCards(card)).CallDeferred();
        }
    }
}
