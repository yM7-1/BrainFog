using BrainFog.Core.Runs;
using MegaCrit.Sts2.Core.Runs;

namespace BrainFog.Game;

/// <summary>
/// Which run mode is active. The game's <c>GameMode</c> enum gained Dread and
/// Blitz in v0.113.0 beta, while this mod still compiles against the 0.107.1
/// reference assembly, so the mode is classified by its enum name at runtime.
/// </summary>
internal static class RunModeContext
{
    public static RunModeKind Kind { get; private set; } = RunModeKind.Unknown;

    public static bool IsBlitz => Kind == RunModeKind.Blitz;

    public static bool IsDread => Kind == RunModeKind.Dread;

    public static bool HasActMap => RunModeRules.HasActMap(Kind);

    public static bool HasGold => RunModeRules.HasGold(Kind);

    public static void Set(RunState? state) =>
        PatchGuard.Run("RunMode.Set", () => Kind = RunModeRules.Parse(state?.GameMode.ToString()));

    /// <summary>Cheap refresh used by the panel; clears the mode after a run
    /// returns to the title screen.</summary>
    public static void Refresh()
    {
        try
        {
            if (!RunManager.Instance.IsInProgress)
            {
                Kind = RunModeKind.Unknown;
                // Leaving a co-op run must clear the panel's multiplayer note
                // (the next lobby re-evaluates the real session type).
                ModRuntime.EvaluateMultiplayer(false);
                return;
            }
            Kind = RunModeRules.Parse(RunManager.Instance.DebugOnlyGetState()?.GameMode.ToString());
        }
        catch
        {
            Kind = RunModeKind.Unknown;
        }
    }
}
