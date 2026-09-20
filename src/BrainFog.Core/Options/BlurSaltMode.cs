namespace BrainFog.Core.Options;

/// <summary>Blur re-roll mode (user change 2026-09-21):
/// - Fixed ("固定混乱"): the garbling never changes across launches;
/// - PerLaunch ("混乱混乱", default): a new garbling on every launch.</summary>
public enum BlurSaltMode
{
    Fixed = 0,
    PerLaunch = 1,
}

public static class BlurSaltModeRules
{
    public static string ToStorage(BlurSaltMode mode) =>
        mode == BlurSaltMode.Fixed ? "fixed" : "per-launch";

    public static BlurSaltMode Parse(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "fixed" => BlurSaltMode.Fixed,
            _ => BlurSaltMode.PerLaunch,
        };

    public static int ToIndex(BlurSaltMode mode) => mode == BlurSaltMode.Fixed ? 0 : 1;

    public static BlurSaltMode FromIndex(int index) =>
        index == 0 ? BlurSaltMode.Fixed : BlurSaltMode.PerLaunch;
}
