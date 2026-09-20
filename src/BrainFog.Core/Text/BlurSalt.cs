namespace BrainFog.Core.Text;

/// <summary>
/// Blur salt (user changes 2026-09-20 and 2026-09-21):
/// - "混乱混乱" (default): a random salt per game launch, so the same source
///   text is garbled differently on every launch ("每次进入游戏都不一样"),
///   while staying stable within one launch so a screen never re-rolls while
///   the player is reading it;
/// - "固定混乱": salt 0, so the garbling never changes across launches.
/// The title screen is included: it uses the same salt-aware blur.
/// </summary>
public static class BlurSalt
{
    /// <summary>Random once per process (= per game launch).</summary>
    private static readonly int LaunchSalt = Random.Shared.Next();

    /// <summary>True = re-roll on every launch (default); false = fixed.</summary>
    public static bool PerLaunch { get; set; } = true;

    /// <summary>Current salt: the launch salt, or 0 in fixed mode.</summary>
    public static int Current => PerLaunch ? LaunchSalt : 0;

    public static int For(string? text) => EventTextBlurrer.StableHash(text ?? string.Empty) ^ Current;
}
