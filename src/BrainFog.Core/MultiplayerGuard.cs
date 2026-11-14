namespace BrainFog.Core;

/// <summary>
/// Multiplayer policy (plan A, 2026-11-14): BrainFog is a local presentation
/// mod. Every peer blinds only itself; knowledge is per-player and no shared
/// run state is written. The one exception is "memory fade", which removes
/// cards from the run deck and is disabled online.
/// </summary>
public static class MultiplayerGuard
{
    /// <summary>BrainFog no longer disables itself in co-op.</summary>
    public static bool IsActiveInSession(bool isMultiplayer) => true;

    /// <summary>Deck-removal rule: single-player only (shared state).</summary>
    public static bool MemoryFadeAllowed(bool isMultiplayer) => !isMultiplayer;

    public const string MultiplayerNotice =
        "多人联机：脑雾只影响你自己；记忆消逝已停用。";

    public const string MultiplayerSetupNotice =
        "多人联机：仅本地表现与本地牌记忆生效，不写入共享对局状态。";
}
