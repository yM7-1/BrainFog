namespace BrainFog.Core.Reveal;

/// <summary>
/// Pure state machine for the "bad memory" mode (user change 2026-09-21):
/// a revealed card copy that enters the hand <c>n</c> times without being played
/// reverts to unknown. Playing resets the counter. The runtime tracks one
/// counter per card copy (identity owner) and persists it with the run.
/// </summary>
public sealed class BadMemoryCounter
{
    /// <summary>Consecutive hand entries that ended without a play.</summary>
    public int UnplayedDraws { get; private set; }

    /// <summary>True once the card was played since its current hand entry.</summary>
    public bool PlayedSinceEntry { get; private set; }

    public void OnEnterHand() => PlayedSinceEntry = false;

    /// <summary>Playing resets the decay counter.</summary>
    public void OnPlayed()
    {
        PlayedSinceEntry = true;
        UnplayedDraws = 0;
    }

    /// <summary>Leaving the hand without a play counts one unplayed draw.
    /// Returns true when the threshold was reached (the caller hides the copy
    /// and the counter resets).</summary>
    public bool OnLeaveHand(int threshold)
    {
        if (PlayedSinceEntry)
        {
            return false;
        }

        UnplayedDraws++;
        if (UnplayedDraws < Math.Max(1, threshold))
        {
            return false;
        }

        UnplayedDraws = 0;
        return true;
    }

    /// <summary>True when leaving the hand right now would reach the threshold,
    /// i.e. the copy is about to be forgotten if it is not played. Drives the
    /// "about to be forgotten" dim hint on the card face (2026-09-21).</summary>
    public bool WouldForgetOnLeave(int threshold) =>
        !PlayedSinceEntry && UnplayedDraws + 1 >= Math.Max(1, threshold);

    /// <summary>Restores a persisted counter value.</summary>
    public void Restore(int unplayedDraws) => UnplayedDraws = Math.Max(0, unplayedDraws);
}
