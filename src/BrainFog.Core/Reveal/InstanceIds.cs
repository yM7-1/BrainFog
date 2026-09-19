using System;

namespace BrainFog.Core.Reveal;

/// <summary>
/// Stable per-instance ids owned by BrainFog. The game has no instance identity
/// for cards, so we mint one and persist it with the run when the difficulty
/// option "reveal same-name cards" is disabled (only the played copy is known).
/// </summary>
public static class InstanceIds
{
    public const string Prefix = "BrainFog.";

    public static string NewId() => Prefix + Guid.NewGuid().ToString("N");

    public static bool IsValid(string? id) =>
        !string.IsNullOrEmpty(id)
        && id.Length > Prefix.Length
        && id.StartsWith(Prefix, StringComparison.Ordinal);
}
