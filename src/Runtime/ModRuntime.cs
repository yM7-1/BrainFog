using BrainFog.Core;
using BrainFog.Core.Reveal;
using MegaCrit.Sts2.Core.Logging;

namespace BrainFog;

/// <summary>
/// Runtime state of the mod for the current session.
/// All gameplay patches must consult <see cref="Disabled"/> before hiding anything.
/// </summary>
public static class ModRuntime
{
    private static bool _isMultiplayer;
    private static bool _userDisabled;

    /// <summary>True while the player turned the mod off in the panel. The mod
    /// is no longer disabled online (plan A, 2026-11-14): multiplayer only
    /// narrows the scope to the local player.</summary>
    public static bool Disabled => _userDisabled;

    /// <summary>True when the player turned the mod off in the modifier panel
    /// (persisted in the mod's own config across launches).</summary>
    public static bool UserDisabled => _userDisabled;

    /// <summary>True when the current run is a multiplayer (co-op) session.</summary>
    public static bool IsMultiplayer => _isMultiplayer;

    /// <summary>Set BRAINFOG_DEBUG=1 to get state dumps in the game log.</summary>
    public static bool DebugEnabled { get; } =
        System.Environment.GetEnvironmentVariable("BRAINFOG_DEBUG") == "1";

    /// <summary>Per-run card knowledge state (spec 0.02 #6, 0.03 a/g).</summary>
    public static CardRevealTracker Tracker { get; } = new();

    public static void EvaluateMultiplayer(bool isMultiplayer)
    {
        if (_isMultiplayer == isMultiplayer)
        {
            return;
        }
        _isMultiplayer = isMultiplayer;
        if (_isMultiplayer)
        {
            Log.Info("[BrainFog] " + MultiplayerGuard.MultiplayerSetupNotice);
        }
    }

    /// <summary>Panel option: player disables / re-enables the whole mod.
    /// The caller applies and persists via <see cref="Game.DifficultyRuntime"/>.</summary>
    public static void SetUserDisabled(bool disabled) => _userDisabled = disabled;

    public static void DumpState(string tag)
    {
        if (!DebugEnabled)
        {
            return;
        }

        var snapshot = Game.SnapshotDisplay.Snapshot;
        Log.Info($"[BrainFog][State:{tag}] disabled={Disabled} multiplayer={_isMultiplayer} userOff={_userDisabled} " +
                 $"revealed={Tracker.RevealedCount} " +
                 $"hp={snapshot.Hp?.ToString() ?? "-"}/{snapshot.MaxHp?.ToString() ?? "-"} gold={snapshot.Gold?.ToString() ?? "-"}");
        var settings = Game.DifficultyRuntime.Current;
        Log.Info($"[BrainFog][Options] blur={Game.DifficultyRuntime.TextBlurPercent}% salt={settings.SaltMode} " +
                 $"memory={settings.MemoryMode} n={settings.BadMemoryThreshold} fade={settings.MemoryFade} " +
                 $"snapshot={settings.SnapshotStatus} readable={settings.ReadableStatusNumbers} " +
                 $"relics={settings.ShowRelics} routes={settings.ShowAllMapRoutes} intent={settings.IntentMode} " +
                 $"models={settings.EnemyModelsVisible} config={Game.DifficultyRuntime.SettingsFilePath}");
    }
}
