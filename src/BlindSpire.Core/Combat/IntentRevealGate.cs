namespace BlindSpire.Core.Combat;

/// <summary>
/// Intent visibility gate (spec 0.03 f): intents show on round 1 only; once the
/// combat passes round 1 the gate stays locked for that creature, even if a phase
/// transition resets the round counter.
/// </summary>
public sealed class IntentRevealGate
{
    private bool _locked;

    public bool IsLocked => _locked;

    /// <summary>Intent shows only for creatures present at combat start, on round 1.</summary>
    public bool ShouldShow(int roundNumber, bool isInitialCombatant)
    {
        if (roundNumber > 1)
        {
            _locked = true;
        }
        return roundNumber == 1 && isInitialCombatant && !_locked;
    }

    public void Reset() => _locked = false;
}
