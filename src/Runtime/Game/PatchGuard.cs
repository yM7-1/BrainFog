using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;

namespace BlindSpire.Game;

/// <summary>
/// Shared exception boundary for patch/game-side code: a broken assumption logs
/// once per key instead of throwing into the game's main loop.
/// </summary>
internal static class PatchGuard
{
    private static readonly HashSet<string> Reported = new();
    private static readonly object Gate = new();

    public static void Run(string key, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Report(key, ex);
        }
    }

    public static T RunOr<T>(string key, Func<T> action, T fallback)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            Report(key, ex);
            return fallback;
        }
    }

    private static void Report(string key, Exception ex)
    {
        lock (Gate)
        {
            if (!Reported.Add(key))
            {
                return;
            }
        }
        Log.Error($"[BlindSpire][{key}] {ex}");
    }
}
