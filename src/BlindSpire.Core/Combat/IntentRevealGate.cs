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

    public bool ShouldShow(int roundNumber)
    {
        if (roundNumber > 1)
        {
            _locked = true;
        }
        return roundNumber == 1 && !_locked;
    }

    public void Reset() => _locked = false;
}
