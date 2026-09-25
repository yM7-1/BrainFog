namespace BrainFog.Core.Status;

/// <summary>
/// Live top-bar HP rendering rules (0.3.7 fix): the Neow heal tween writes the
/// true HP straight into the top bar, so it must only be suppressed while the
/// mod is active AND "HP/gold amnesia mode" shows the stale snapshot. In live
/// mode (the default since 0.3.2) the tween has to run, otherwise the bar stays
/// at "0/max" for the whole Neow event.
/// </summary>
public static class StatusDisplayRules
{
    public static bool AllowsLiveHpUpdates(bool modDisabled, bool snapshotStatus) =>
        modDisabled || !snapshotStatus;
}
