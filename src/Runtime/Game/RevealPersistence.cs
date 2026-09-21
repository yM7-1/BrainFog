using BrainFog.Core.Reveal;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.RunData;

namespace BrainFog.Game;

/// <summary>
/// Persists reveal knowledge with the run save (per-run memory). Definition
/// reveals are plain keys; per-instance reveals (difficulty option) are rebound
/// from the saved deck order on load.
/// </summary>
internal static class RevealPersistence
{
    private static RunSavedData<BrainFogRunData>? _slot;
    private static WeakReference<RunState>? _runStateRef;
    private static readonly List<string> PendingDeckOrder = new();
    private static bool _refreshing;

    public static void Register()
    {
        _slot ??= RunSavedDataStore.For("BrainFog").Register<BrainFogRunData>("reveal");
    }

    public static void OnRunStarted(RunState state) =>
        PatchGuard.Run("Persistence.OnRunStarted", () => OnRunStartedCore(state));

    private static void OnRunStartedCore(RunState state)
    {
        _runStateRef = new WeakReference<RunState>(state);
        PendingDeckOrder.Clear();
        if (_slot != null && _slot.TryGet(state, out var data))
        {
            ModRuntime.Tracker.Load(data.RevealedCards, data.RevealedInstances);
            PendingDeckOrder.AddRange(data.DeckOrderIds);
            BindDeckFromSave(state, data);
            BadMemoryTracker.OnRunLoaded(data.BadMemoryCounts);
            PlayCounterTracker.OnRunLoaded(data.PlayCounts, data.CardNumbers, data.CardNames, data.NextCardNumbers);
            PlayCounterTracker.EnsureDeckNumbers(state);
        }
        else
        {
            ModRuntime.Tracker.Reset();
            BadMemoryTracker.OnRunLoaded(null);
            PlayCounterTracker.OnRunLoaded(null, null, null, null);
            InitialReveal.Apply(state);
        }
        MegaCrit.Sts2.Core.Logging.Log.Info(
            $"[BrainFog][Persistence] run started: revealed={ModRuntime.Tracker.RevealedCount} savedDeck={PendingDeckOrder.Count}");
        ModRuntime.DumpState("run-started");
    }

    /// <summary>Rebinds saved instance ids to the loaded deck immediately
    /// (save/load fix 2026-09-21). Rendering consults the registry right away,
    /// so lazy binding at reveal time was too late and every instance reveal
    /// looked unknown after a reload.</summary>
    private static void BindDeckFromSave(RunState state, BrainFogRunData data)
    {
        if (data.DeckOrderIds.Count == 0 || state.Players.Count == 0)
        {
            return;
        }

        var bound = 0;
        foreach (var player in state.Players)
        {
            var deck = player.Deck.Cards;
            if (deck.Count == 0)
            {
                continue;
            }

            var keys = new List<string>(deck.Count);
            foreach (var card in deck)
            {
                keys.Add(RevealKeys.Of(card));
            }

            foreach (var binding in DeckRebinder.Align(keys, data.DeckOrderIds, data.DeckOrderKeys))
            {
                if (binding.DeckIndex >= 0 && binding.DeckIndex < deck.Count)
                {
                    CardInstanceRegistry.Bind(deck[binding.DeckIndex], binding.InstanceId);
                    bound++;
                }
            }
        }

        if (bound > 0)
        {
            // Eager binding covered the saved order; disable index-based lazy
            // rebinding so genuinely new cards mint fresh ids instead of
            // stealing a saved slot.
            PendingDeckOrder.Clear();
        }
    }

    /// <summary>Rebinds an instance id from the saved deck order (index-aligned).</summary>
    public static bool TryBindFromSave(CardModel card)
    {
        if (PendingDeckOrder.Count == 0 || card.Owner is not { } owner)
        {
            return false;
        }

        var deck = owner.Deck.Cards;
        for (var i = 0; i < deck.Count; i++)
        {
            if (!ReferenceEquals(deck[i], card))
            {
                continue;
            }

            if (DeckOrderBinding.TryResolve(i, PendingDeckOrder, out var id, out _))
            {
                CardInstanceRegistry.Bind(card, id);
                return true;
            }
            return false; // fail closed: keep the instance unknown rather than mis-bind
        }
        return false;
    }
    /// <summary>Writes the current deck order (id per slot) into the run save.</summary>
    public static void RefreshDeckOrder(Player owner) =>
        PatchGuard.Run("Persistence.RefreshDeckOrder", () => RefreshDeckOrderCore(owner));

    private static void RefreshDeckOrderCore(Player owner)
    {
        if (_refreshing || _slot == null || owner.RunState is not RunState state || !IsCurrentRun(state))
        {
            return;
        }

        _refreshing = true;
        try
        {
            var ids = new List<string>();
            var keys = new List<string>();
            foreach (var card in owner.Deck.Cards)
            {
                ids.Add(CardInstanceRegistry.PeekOrBindNew(card));
                keys.Add(RevealKeys.Of(card));
            }

            if (_slot.TryGet(state, out var current)
                && ListEquals(current.DeckOrderIds, ids)
                && ListEquals(current.DeckOrderKeys, keys))
            {
                return; // nothing changed: avoid dirtying the run save
            }
            _slot.Modify(state, data =>
            {
                data.DeckOrderIds = ids;
                data.DeckOrderKeys = keys;
            });
        }
        finally
        {
            _refreshing = false;
        }
    }

    private static bool ListEquals(List<string> a, List<string> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }
        for (var i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsCurrentRun(RunState state) =>
        _runStateRef != null && _runStateRef.TryGetTarget(out var current) && ReferenceEquals(current, state);

    /// <summary>Adds a revealed card definition to the run save.</summary>
    public static void OnRevealed(string definitionKey) =>
        PatchGuard.Run("Persistence.OnRevealed", () => OnRevealedCore(definitionKey));

    private static void OnRevealedCore(string definitionKey)
    {
        if (_slot == null || _runStateRef == null || !_runStateRef.TryGetTarget(out var state))
        {
            return;
        }

        _slot.Modify(state, data =>
        {
            if (!data.RevealedCards.Contains(definitionKey))
            {
                data.RevealedCards.Add(definitionKey);
            }
        });
    }

    /// <summary>Adds a revealed card instance (and its deck order) to the run save.</summary>
    public static void OnInstanceRevealed(CardModel card, string instanceId) =>
        PatchGuard.Run("Persistence.OnInstanceRevealed", () => OnInstanceRevealedCore(card, instanceId));

    private static void OnInstanceRevealedCore(CardModel card, string instanceId)
    {
        if (_slot == null || _runStateRef == null || !_runStateRef.TryGetTarget(out var state))
        {
            return;
        }

        // Keep the saved deck order in sync with the deck as of this reveal, so
        // index binding after a load cannot drift (bug fix 2026-09-20).
        if (card.Owner is { } owner)
        {
            RefreshDeckOrderCore(owner);
        }

        _slot.Modify(state, data =>
        {
            if (!data.RevealedInstances.Contains(instanceId))
            {
                data.RevealedInstances.Add(instanceId);
            }
        });
    }

    /// <summary>Batch-persists the starting-deck reveal of a fresh run
    /// (2026-09-21: the starting deck counts as known per memory mode).</summary>
    public static void OnInitialReveal(
        Player owner,
        IReadOnlyCollection<string> definitionKeys,
        IReadOnlyCollection<string> instanceIds) =>
        PatchGuard.Run("Persistence.InitialReveal", () => OnInitialRevealCore(owner, definitionKeys, instanceIds));

    private static void OnInitialRevealCore(
        Player owner,
        IReadOnlyCollection<string> definitionKeys,
        IReadOnlyCollection<string> instanceIds)
    {
        if (definitionKeys.Count == 0 && instanceIds.Count == 0)
        {
            return;
        }
        if (_slot == null || _runStateRef == null || !_runStateRef.TryGetTarget(out var state))
        {
            return;
        }

        RefreshDeckOrderCore(owner);
        _slot.Modify(state, data =>
        {
            foreach (var key in definitionKeys)
            {
                if (!data.RevealedCards.Contains(key))
                {
                    data.RevealedCards.Add(key);
                }
            }
            foreach (var id in instanceIds)
            {
                if (!data.RevealedInstances.Contains(id))
                {
                    data.RevealedInstances.Add(id);
                }
            }
        });
    }

    /// <summary>"Play counter": persists one copy's play count.</summary>
    public static void SetPlayCount(string instanceId, int count) =>
        PatchGuard.Run("Persistence.PlayCount", () => SetPlayCountCore(instanceId, count));

    private static void SetPlayCountCore(string instanceId, int count)
    {
        if (string.IsNullOrEmpty(instanceId) || count <= 0
            || _slot == null || _runStateRef == null || !_runStateRef.TryGetTarget(out var state)
            || !IsCurrentRun(state))
        {
            return;
        }

        _slot.Modify(state, data => data.PlayCounts[instanceId] = count);
    }

    /// <summary>"Play counter": persists a copy's stable number and name.</summary>
    public static void SetCardNumber(string instanceId, int number, string name) =>
        PatchGuard.Run("Persistence.CardNumber", () => SetCardNumberCore(instanceId, number, name));

    private static void SetCardNumberCore(string instanceId, int number, string name)
    {
        if (string.IsNullOrEmpty(instanceId) || number <= 0
            || _slot == null || _runStateRef == null || !_runStateRef.TryGetTarget(out var state)
            || !IsCurrentRun(state))
        {
            return;
        }

        _slot.Modify(state, data =>
        {
            data.CardNumbers[instanceId] = number;
            if (!string.IsNullOrEmpty(name))
            {
                data.CardNames[instanceId] = name;
            }
        });
    }

    /// <summary>"Play counter": persists the next number per definition so
    /// numbering stays stable when copies leave the deck.</summary>
    public static void SetNextCardNumber(string definitionKey, int next) =>
        PatchGuard.Run("Persistence.NextCardNumber", () => SetNextCardNumberCore(definitionKey, next));

    private static void SetNextCardNumberCore(string definitionKey, int next)
    {
        if (string.IsNullOrEmpty(definitionKey) || next <= 0
            || _slot == null || _runStateRef == null || !_runStateRef.TryGetTarget(out var state)
            || !IsCurrentRun(state))
        {
            return;
        }

        _slot.Modify(state, data => data.NextCardNumbers[definitionKey] = next);
    }

    /// <summary>Cards left the deck (memory fade): drop their saved reveal,
    /// counter and play-counter entries; the deck order is refreshed too.</summary>
    public static void OnCardsRemoved(Player owner, IReadOnlyCollection<string> instanceIds) =>
        PatchGuard.Run("Persistence.CardsRemoved", () => OnCardsRemovedCore(owner, instanceIds));

    private static void OnCardsRemovedCore(Player owner, IReadOnlyCollection<string> instanceIds)
    {
        if (_slot == null || _runStateRef == null || !_runStateRef.TryGetTarget(out var state))
        {
            return;
        }

        RefreshDeckOrderCore(owner);
        if (instanceIds.Count == 0)
        {
            return;
        }

        _slot.Modify(state, data =>
        {
            foreach (var id in instanceIds)
            {
                data.RevealedInstances.Remove(id);
                data.BadMemoryCounts?.Remove(id);
                data.PlayCounts.Remove(id);
                data.CardNumbers.Remove(id);
                data.CardNames.Remove(id);
            }
        });
    }

    /// <summary>Persists a "bad memory" counter (0 removes the entry).</summary>
    public static void SetBadMemoryCounter(string instanceId, int count) =>
        PatchGuard.Run("Persistence.BadMemory", () => SetBadMemoryCounterCore(instanceId, count));

    private static void SetBadMemoryCounterCore(string instanceId, int count)
    {
        if (_slot == null || _runStateRef == null || !_runStateRef.TryGetTarget(out var state))
        {
            return;
        }

        _slot.Modify(state, data =>
        {
            data.BadMemoryCounts ??= new Dictionary<string, int>();
            if (count <= 0)
            {
                data.BadMemoryCounts.Remove(instanceId);
            }
            else
            {
                data.BadMemoryCounts[instanceId] = count;
            }
        });
    }

    /// <summary>"Bad memory": the copy reverted to unknown; drop it from the save.</summary>
    public static void OnInstanceHidden(string instanceId) =>
        PatchGuard.Run("Persistence.InstanceHidden", () => OnInstanceHiddenCore(instanceId));

    private static void OnInstanceHiddenCore(string instanceId)
    {
        if (_slot == null || _runStateRef == null || !_runStateRef.TryGetTarget(out var state))
        {
            return;
        }

        _slot.Modify(state, data =>
        {
            data.RevealedInstances.Remove(instanceId);
            data.BadMemoryCounts?.Remove(instanceId);
        });
    }
}
