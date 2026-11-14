namespace BrainFog.Core.Runs;

/// <summary>
/// Slay the Spire 2 run modes without a compile-time dependency on the game's
/// <c>GameMode</c> enum: the mod still compiles against the 0.107.1 reference
/// assembly, which predates Blitz and Dread (v0.113.0 beta).
/// </summary>
public enum RunModeKind
{
    Unknown,
    Standard,
    Daily,
    Custom,
    Dread,
    Blitz,
}

public static class RunModeRules
{
    /// <summary>Parses the value of <c>IRunState.GameMode.ToString()</c>.</summary>
    public static RunModeKind Parse(string? gameModeName) => gameModeName?.Trim().ToLowerInvariant() switch
    {
        "standard" => RunModeKind.Standard,
        "daily" => RunModeKind.Daily,
        "custom" => RunModeKind.Custom,
        "dread" => RunModeKind.Dread,
        "blitz" => RunModeKind.Blitz,
        _ => RunModeKind.Unknown,
    };

    /// <summary>Blitz has no act map; it uses the compact top-bar progress map.</summary>
    public static bool HasActMap(RunModeKind kind) => kind != RunModeKind.Blitz;

    /// <summary>Blitz has no gold economy; the shop uses Blitz choices instead.</summary>
    public static bool HasGold(RunModeKind kind) => kind != RunModeKind.Blitz;

    /// <summary>Dread enables every ascension modifier and adds dominion buffs.</summary>
    public static bool UsesDominion(RunModeKind kind) => kind == RunModeKind.Dread;

    /// <summary>
    /// Multiplayer is local-only (plan A): every peer blinds itself and no
    /// shared run state is written. "Memory fade" removes cards from the run
    /// deck and is therefore disabled online.
    /// </summary>
    public static bool MemoryFadeAllowed(bool isMultiplayer) => !isMultiplayer;
}
