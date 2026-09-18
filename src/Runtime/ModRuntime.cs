using BlindSpire.Core;
using BlindSpire.Core.Reveal;
using MegaCrit.Sts2.Core.Logging;

namespace BlindSpire;

/// <summary>
/// Runtime state of the mod for the current session.
/// All gameplay patches must consult <see cref="Disabled"/> before hiding anything.
/// </summary>
public static class ModRuntime
{
    public static bool Disabled { get; private set; }

    /// <summary>Set BLINDSPIRE_DEBUG=1 to get state dumps in the game log.</summary>
    public static bool DebugEnabled { get; } =
        System.Environment.GetEnvironmentVariable("BLINDSPIRE_DEBUG") == "1";

    /// <summary>Per-run card knowledge state (spec 0.02 #6, 0.03 a/g).</summary>
    public static CardRevealTracker Tracker { get; } = new();

    public static void EvaluateMultiplayer(bool isMultiplayer)
    {
        Disabled = MultiplayerGuard.ShouldDisable(isMultiplayer);
        if (Disabled)
        {
            Log.Info("[BlindSpire] " + MultiplayerGuard.DisabledReason);
        }
    }

    public static void DumpState(string tag)
    {
        if (!DebugEnabled)
        {
            return;
        }

        var snapshot = Game.SnapshotDisplay.Snapshot;
        Log.Info($"[BlindSpire][State:{tag}] disabled={Disabled} revealed={Tracker.RevealedCount} " +
                 $"hp={snapshot.Hp?.ToString() ?? "-"}/{snapshot.MaxHp?.ToString() ?? "-"} gold={snapshot.Gold?.ToString() ?? "-"}");
    }
}
