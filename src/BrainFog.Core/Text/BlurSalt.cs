namespace BrainFog.Core.Text;

/// <summary>
/// Per-launch blur salt (user change 2026-09-20): the same source text is
/// garbled differently on every launch of the game ("每次进入游戏都不一样"),
/// while staying stable within one launch so a screen never re-rolls while the
/// player is reading it. The title screen is included: it uses the same
/// salt-aware blur as everything else.
/// </summary>
public static class BlurSalt
{
    /// <summary>Random once per process (= per game launch).</summary>
    public static int Current { get; } = Random.Shared.Next();

    public static int For(string? text) => EventTextBlurrer.StableHash(text ?? string.Empty) ^ Current;
}
