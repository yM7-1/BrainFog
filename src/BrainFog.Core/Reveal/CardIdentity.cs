namespace BrainFog.Core.Reveal;

/// <summary>
/// Instance-identity rule (bug fix 2026-09-20): the game clones every deck card
/// into the draw pile at combat start (Player.PopulateCombatState) and the clone
/// carries DeckVersion pointing back at the run-level deck card. Effect clones
/// (Anger/DualWield/etc.) carry CloneOf pointing at their source. Identity must
/// follow these links back to the run-level card, otherwise each combat's fresh
/// clones mint new ids and the per-instance reveal resets every combat.
/// </summary>
public static class CardIdentity
{
    /// <summary>Maximum chain hops before giving up (cycle/abuse guard).</summary>
    public const int MaxDepth = 8;

    /// <summary>
    /// Walks <paramref name="parentOf"/> until it returns null, returning the last
    /// non-null card. A cycle longer than <see cref="MaxDepth"/> stops at the
    /// depth limit so a broken graph can never hang the game.
    /// </summary>
    public static T Resolve<T>(T card, Func<T, T?> parentOf) where T : class
    {
        var current = card;
        for (var depth = 0; depth < MaxDepth; depth++)
        {
            var parent = parentOf(current);
            if (parent == null || ReferenceEquals(parent, current))
            {
                return current;
            }
            current = parent;
        }
        return current;
    }
}
