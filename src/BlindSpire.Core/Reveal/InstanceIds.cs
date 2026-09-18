using System;

namespace BlindSpire.Core.Reveal;

/// <summary>
/// Stable per-instance ids owned by BlindSpire. The game has no instance identity for
/// cards, so we mint one and persist it with the run (see CardRevealTracker).
/// </summary>
public static class InstanceIds
{
    public const string Prefix = "BlindSpire.";

    public static string NewId() => Prefix + Guid.NewGuid().ToString("N");

    public static bool IsValid(string? id) =>
        !string.IsNullOrEmpty(id)
        && id.Length > Prefix.Length
        && id.StartsWith(Prefix, StringComparison.Ordinal);
}
