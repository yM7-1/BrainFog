using BlindSpire.Core.Reveal;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.RunData;

namespace BlindSpire.Game;

/// <summary>
/// Persists reveal knowledge with the run save (spec 0.02 #5: per-run memory).
/// Uses RitsuLib's RunSavedDataStore; instance ids are rebound by deck order on load.
/// </summary>
internal static class RevealPersistence
{
    private static RunSavedData<BlindSpireRunData>? _slot;
    private static WeakReference<RunState>? _runStateRef;
    private static readonly List<string> PendingDeckOrder = new();
    private static bool _refreshing;

    public static void Register()
    {
        _slot ??= RunSavedDataStore.For("BlindSpire").Register<BlindSpireRunData>("reveal");
    }

    public static void OnRunStarted(RunState state) =>
        PatchGuard.Run("Persistence.OnRunStarted", () => OnRunStartedCore(state));

    private static void OnRunStartedCore(RunState state)
    {
        _runStateRef = new WeakReference<RunState>(state);
        PendingDeckOrder.Clear();
        if (_slot != null && _slot.TryGet(state, out var data))
        {
            ModRuntime.Tracker.Load(data.RevealedIds);
            PendingDeckOrder.AddRange(data.DeckOrderIds);
        }
        else
        {
            ModRuntime.Tracker.Reset();
        }
        MegaCrit.Sts2.Core.Logging.Log.Info(
            $"[BlindSpire][Persistence] run started: revealed={ModRuntime.Tracker.RevealedCount} savedDeck={PendingDeckOrder.Count}");
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

    public static void OnRevealed(CardModel card, string id) =>
        PatchGuard.Run("Persistence.OnRevealed", () => OnRevealedCore(card, id));

    private static void OnRevealedCore(CardModel card, string id)
    {
        if (_slot == null || card.RunState is not RunState state || !IsCurrentRun(state))
        {
            return;
        }

        _slot.Modify(state, data =>
        {
            if (!data.RevealedIds.Contains(id))
            {
                data.RevealedIds.Add(id);
            }
        });
    }
}
