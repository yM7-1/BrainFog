namespace BrainFog.Core.Options;

/// <summary>Enemy intent visibility (user change 2026-09-21, replaces the old
/// "show all intents" toggle).</summary>
public enum IntentVisibility
{
    Hidden = 0,
    FirstRoundOnly = 1,
    All = 2,
}

public static class IntentVisibilityRules
{
    public static string ToStorage(IntentVisibility mode) => mode switch
    {
        IntentVisibility.All => "all",
        IntentVisibility.FirstRoundOnly => "first",
        _ => "hidden",
    };

    public static IntentVisibility Parse(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "all" => IntentVisibility.All,
            "first" => IntentVisibility.FirstRoundOnly,
            _ => IntentVisibility.Hidden,
        };

    public static int ToIndex(IntentVisibility mode) => (int)mode;

    public static IntentVisibility FromIndex(int index) => index switch
    {
        2 => IntentVisibility.All,
        1 => IntentVisibility.FirstRoundOnly,
        _ => IntentVisibility.Hidden,
    };

    /// <summary>Combines the mode with the first-round gate result.</summary>
    public static bool ShouldShow(IntentVisibility mode, bool firstRoundGate) => mode switch
    {
        IntentVisibility.All => true,
        IntentVisibility.FirstRoundOnly => firstRoundGate,
        _ => false,
    };
}
