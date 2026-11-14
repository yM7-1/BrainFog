using BrainFog.Core.Reveal;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.RunData;

namespace BrainFog.Game;

/// <summary>
/// Persists reveal knowledge with the run save (per-run memory). Definition
/// reveals are plain keys; per-instance reveals (difficulty option) are rebound
/// from the saved deck order on load.
///
/// Multiplayer (plan A, 2026-11-14): knowledge is stored in a per-player slot
/// keyed by NetId. Remote players' cards never enter the local tracker, so
/// there is no shared knowledge state to desync. Single-player saves are
/// migrated from the old run-shared slot on first load.
/// </summary>
internal static class RevealPersistence
{
    private const string LegacySlotKey = "reveal";
    private const string PlayerSlotKey = "reveal_player";

    private static PlayerRunSavedData<BrainFogRunData>? _slot;
    private static RunSavedData<BrainFogRunData>? _legacySlot;
    private static WeakReference<RunState>? _runStateRef;
    private static Player? _localPlayer;
    private static readonly List<string> PendingDeckOrder = new();
    private static bool _refreshing;

    public static void Register()
    {
        var store = RunSavedDataStore.For("BrainFog");
        _slot ??= store.RegisterPerPlayer<BrainFogRunData>(PlayerSlotKey);
        _legacySlot ??= store.Register<BrainFogRunData>(LegacySlotKey);
    }

    public static void OnRunStarted(RunState state) =>
        PatchGuard.Run("Persistence.OnRunStarted", () => OnRunStartedCore(state));

    private static void OnRunStartedCore(RunState state)
    {
        _runStateRef = new WeakReference<RunState>(state);
        _localPlayer = ResolveLocalPlayer(state);
        RunModeContext.Set(state);
        PendingDeckOrder.Clear();

        var data = LoadLocalData(state);
        if (data != null)
        {
            ModRuntime.Tracker.Load(data.RevealedCards, data.RevealedInstances);
            PendingDeckOrder.AddRange(data.DeckOrderIds);
            BindDeckFromSave(data);
            BadMemoryTracker.OnRunLoaded(data.BadMemoryCounts);
        }
        else
        {
            ModRuntime.Tracker.Reset();
            BadMemoryTracker.OnRunLoaded(null);
        }

        // A freshly created per-player slot (RitsuLib lobby staging) is empty;
        // its presence must not suppress the starting-deck reveal.
        if (data == null || !data.InitialRevealApplied)
        {
            InitialReveal.Apply(state);
        }
        Log.Info(
            $"[BrainFog][Persistence] run started: multiplayer={ModRuntime.IsMultiplayer} " +
            $"revealed={ModRuntime.Tracker.RevealedCount} savedDeck={PendingDeckOrder.Count}");
        ModRuntime.DumpState("run-started");
    }

    private static Player? ResolveLocalPlayer(RunState state)
    {
        var me = LocalContext.GetMe(state);
        if (me != null)
        {
            return me;
        }
        // Single-player always has a player even if NetId was not assigned yet;
        // multiplayer must fail closed rather than guess another player's data.
        return ModRuntime.IsMultiplayer ? null : state.Players.FirstOrDefault();
    }

    /// <summary>Loads the local player's per-player slot; in single-player,
    /// migrates an old run-shared slot from pre-0.4.0 saves.</summary>
    private static BrainFogRunData? LoadLocalData(RunState state)
    {
        if (_localPlayer == null)
        {
            return null;
        }

        if (_slot != null && _slot.TryGet(state, _localPlayer.NetId, out var data))
        {
            return data;
        }

        if (ModRuntime.IsMultiplayer || _legacySlot == null || !_legacySlot.TryGet(state, out var legacy))
        {
            return null;
        }

        legacy.InitialRevealApplied = true; // pre-0.4.0 fresh runs already did this
        _slot?.Set(state, _localPlayer.NetId, legacy);
        Log.Info("[BrainFog][Persistence] migrated legacy single-player save to per-player slot");
        return legacy;
    }

    /// <summary>Rebinds saved instance ids to the loaded deck immediately
    /// (save/load fix 2026-09-21). Rendering consults the registry right away,
    /// so lazy binding at reveal time was too late and every instance reveal
    /// looked unknown after a reload. Only the local player's deck is bound.</summary>
    private static void BindDeckFromSave(BrainFogRunData data)
    {
        if (data.DeckOrderIds.Count == 0 || _localPlayer == null)
        {
            return;
        }

        var deck = _localPlayer.Deck.Cards;
        if (deck.Count == 0)
        {
            return;
        }

        var keys = new List<string>(deck.Count);
        foreach (var card in deck)
        {
            keys.Add(RevealKeys.Of(card));
        }

        var bound = 0;
        foreach (var binding in DeckRebinder.Align(keys, data.DeckOrderIds, data.DeckOrderKeys))
        {
            if (binding.DeckIndex >= 0 && binding.DeckIndex < deck.Count)
            {
                CardInstanceRegistry.Bind(deck[binding.DeckIndex], binding.InstanceId);
                bound++;
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
        if (PendingDeckOrder.Count == 0 || card.Owner is not { } owner || LocalScope.IsRemoteCard(card))
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

    /// <summary>Writes the local player's current deck order (id per slot) into
    /// the per-player run save.</summary>
    public static void RefreshDeckOrder(Player owner) =>
        PatchGuard.Run("Persistence.RefreshDeckOrder", () => RefreshDeckOrderCore(owner));

    private static void RefreshDeckOrderCore(Player owner)
    {
        if (_refreshing
            || _slot == null
            || _localPlayer == null
            || !LocalContext.IsMe(owner)
            || owner.RunState is not RunState state
            || !IsCurrentRun(state))
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

            if (_slot.TryGet(state, _localPlayer.NetId, out var current)
                && ListEquals(current.DeckOrderIds, ids)
                && ListEquals(current.DeckOrderKeys, keys))
            {
                return; // nothing changed: avoid dirtying the run save
            }
            _slot.Modify(_localPlayer, data =>
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

    private static bool TryModifyLocal(Action<BrainFogRunData> mutate)
    {
        if (_slot == null
            || _localPlayer == null
            || _runStateRef == null
            || !_runStateRef.TryGetTarget(out var state)
            || !IsCurrentRun(state))
        {
            return false;
        }
        _slot.Modify(_localPlayer, mutate);
        return true;
    }

    /// <summary>Adds a revealed card definition to the local player's save data.</summary>
    public static void OnRevealed(string definitionKey) =>
        PatchGuard.Run("Persistence.OnRevealed", () => OnRevealedCore(definitionKey));

    private static void OnRevealedCore(string definitionKey) =>
        TryModifyLocal(data =>
        {
            if (!data.RevealedCards.Contains(definitionKey))
            {
                data.RevealedCards.Add(definitionKey);
            }
        });

    /// <summary>Adds a revealed card instance (and its deck order) to the save.</summary>
    public static void OnInstanceRevealed(CardModel card, string instanceId) =>
        PatchGuard.Run("Persistence.OnInstanceRevealed", () => OnInstanceRevealedCore(card, instanceId));

    private static void OnInstanceRevealedCore(CardModel card, string instanceId)
    {
        if (LocalScope.IsRemoteCard(card))
        {
            return;
        }

        // Keep the saved deck order in sync with the deck as of this reveal, so
        // index binding after a load cannot drift (bug fix 2026-09-20).
        if (card.Owner is { } owner)
        {
            RefreshDeckOrderCore(owner);
        }

        TryModifyLocal(data =>
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
        if (LocalScope.IsRemotePlayer(owner))
        {
            return;
        }

        RefreshDeckOrderCore(owner);
        TryModifyLocal(data =>
        {
            data.InitialRevealApplied = true;
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

    /// <summary>Cards left the deck (memory fade): drop their saved reveal and
    /// counter entries; the deck order is refreshed too.</summary>
    public static void OnCardsRemoved(Player owner, IReadOnlyCollection<string> instanceIds) =>
        PatchGuard.Run("Persistence.CardsRemoved", () => OnCardsRemovedCore(owner, instanceIds));

    private static void OnCardsRemovedCore(Player owner, IReadOnlyCollection<string> instanceIds)
    {
        if (LocalScope.IsRemotePlayer(owner))
        {
            return;
        }

        RefreshDeckOrderCore(owner);
        if (instanceIds.Count == 0)
        {
            return;
        }

        TryModifyLocal(data =>
        {
            foreach (var id in instanceIds)
            {
                data.RevealedInstances.Remove(id);
                data.BadMemoryCounts?.Remove(id);
            }
        });
    }

    /// <summary>Persists a "bad memory" counter (0 removes the entry).</summary>
    public static void SetBadMemoryCounter(string instanceId, int count) =>
        PatchGuard.Run("Persistence.BadMemory", () => SetBadMemoryCounterCore(instanceId, count));

    private static void SetBadMemoryCounterCore(string instanceId, int count) =>
        TryModifyLocal(data =>
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

    /// <summary>"Bad memory": the copy reverted to unknown; drop it from the save.</summary>
    public static void OnInstanceHidden(string instanceId) =>
        PatchGuard.Run("Persistence.InstanceHidden", () => OnInstanceHiddenCore(instanceId));

    private static void OnInstanceHiddenCore(string instanceId) =>
        TryModifyLocal(data =>
        {
            data.RevealedInstances.Remove(instanceId);
            data.BadMemoryCounts?.Remove(instanceId);
        });
}
