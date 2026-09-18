namespace BlindSpire.Core;

/// <summary>
/// BlindSpire only supports single-player runs (spec 0.02 #10).
/// The game layer reports the multiplayer flag; this stays pure and testable.
/// </summary>
public static class MultiplayerGuard
{
    public static bool ShouldDisable(bool isMultiplayer) => isMultiplayer;

    public const string DisabledReason = "BlindSpire 仅支持单人模式，联机对局已自动禁用。";
}
