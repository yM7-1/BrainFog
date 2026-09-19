using BrainFog.Core.Reveal;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.RunData;

namespace BrainFog.Game;

/// <summary>
/// Persists reveal knowledge with the run save (spec 0.02 #5: per-run memory).
/// Knowledge is keyed by card definition (user change 2026-09-19), so the save
/// can be a plain set of model ids — no deck-order rebinding required.
/// </summary>
internal static class RevealPersistence
{
    private static RunSavedData<BrainFogRunData>? _slot;
    private static WeakReference<RunState>? _runStateRef;

    public static void Register()
    {
        _slot ??= RunSavedDataStore.For("BrainFog").Register<BrainFogRunData>("reveal");
    }

    public static void OnRunStarted(RunState state) =>
        PatchGuard.Run("Persistence.OnRunStarted", () => OnRunStartedCore(state));

    private static void OnRunStartedCore(RunState state)
    {
        _runStateRef = new WeakReference<RunState>(state);
        if (_slot != null && _slot.TryGet(state, out var data))
        {
            ModRuntime.Tracker.Load(data.RevealedCards);
        }
        else
        {
            ModRuntime.Tracker.Reset();
        }
        MegaCrit.Sts2.Core.Logging.Log.Info(
            $"[BrainFog][Persistence] run started: revealed={ModRuntime.Tracker.RevealedCount}");
        ModRuntime.DumpState("run-started");
    }

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
}
