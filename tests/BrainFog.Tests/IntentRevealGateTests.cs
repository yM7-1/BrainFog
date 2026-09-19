using BrainFog.Core.Combat;
using Xunit;

namespace BrainFog.Tests;

public class IntentRevealGateTests
{
    [Fact]
    public void FirstRound_Shows()
    {
        var gate = new IntentRevealGate();
        Assert.True(gate.ShouldShow(1, true));
        Assert.False(gate.IsLocked);
    }

    [Fact]
    public void AfterRoundOne_StaysLockedEvenIfRoundResets()
    {
        var gate = new IntentRevealGate();
        Assert.True(gate.ShouldShow(1, true));
        Assert.False(gate.ShouldShow(2, true));
        Assert.True(gate.IsLocked);
        Assert.False(gate.ShouldShow(1, true)); // phase transition resetting the counter
    }

    [Fact]
    public void FirstSeenAfterRoundOne_NeverShows()
    {
        var gate = new IntentRevealGate();
        Assert.False(gate.ShouldShow(5, true));
        Assert.True(gate.IsLocked);
    }

    [Fact]
    public void SummonedCreature_NeverShows()
    {
        var gate = new IntentRevealGate();
        Assert.False(gate.ShouldShow(1, isInitialCombatant: false));
    }

    [Fact]
    public void Reset_UnlocksForANewCombat()
    {
        var gate = new IntentRevealGate();
        gate.ShouldShow(3, true);
        Assert.True(gate.IsLocked);
        gate.Reset();
        Assert.False(gate.IsLocked);
        Assert.True(gate.ShouldShow(1, true));
    }
}
