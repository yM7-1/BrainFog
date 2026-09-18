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
}
