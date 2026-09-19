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
        }
        else
        {
            ModRuntime.Tracker.Reset();
        }
        MegaCrit.Sts2.Core.Logging.Log.Info(
            $"[BrainFog][Persistence] run started: revealed={ModRuntime.Tracker.RevealedCount} savedDeck={PendingDeckOrder.Count}");
        ModRuntime.DumpState("run-started");
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
            foreach (var card in owner.Deck.Cards)
            {
                ids.Add(CardInstanceRegistry.PeekOrBindNew(card));
            }

            if (_slot.TryGet(state, out var current) && DeckOrderEquals(current.DeckOrderIds, ids))
            {
                return; // nothing changed: avoid dirtying the run save
            }
            _slot.Modify(state, data => data.DeckOrderIds = ids);
        }
        finally
        {
            _refreshing = false;
        }
    }

    private static bool DeckOrderEquals(List<string> a, List<string> b)
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
}
