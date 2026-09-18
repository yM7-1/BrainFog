using BlindSpire.Core.Combat;
using BlindSpire.Core.Reveal;
using Xunit;

namespace BlindSpire.Tests;

public class DeckOrderBindingTests
{
    private static readonly string A = InstanceIds.NewId();
    private static readonly string B = InstanceIds.NewId();
    private static readonly string C = InstanceIds.NewId();

    [Fact]
    public void TryResolve_ReturnsIdForMatchingIndex()
    {
        Assert.True(DeckOrderBinding.TryResolve(1, new[] { A, B, C }, out var id, out var error));
        Assert.Equal(B, id);
        Assert.Null(error);
    }

    [Fact]
    public void TryResolve_FailsClosedOnOutOfRange()
    {
        Assert.False(DeckOrderBinding.TryResolve(3, new[] { A, B, C }, out _, out var error));
        Assert.Equal("deck index out of range", error);
        Assert.False(DeckOrderBinding.TryResolve(-1, new[] { A }, out _, out _));
    }

    [Fact]
    public void TryResolve_FailsClosedOnEmptyOrNullSavedOrder()
    {
        Assert.False(DeckOrderBinding.TryResolve(0, Array.Empty<string>(), out _, out _));
        Assert.False(DeckOrderBinding.TryResolve(0, null, out _, out _));
    }

    [Fact]
    public void TryResolve_FailsClosedOnInvalidId()
    {
        Assert.False(DeckOrderBinding.TryResolve(0, new[] { "not-an-id" }, out _, out var error));
        Assert.Equal("invalid instance id", error);
    }

    [Fact]
    public void TryResolve_FailsClosedOnDuplicateIds()
    {
        Assert.False(DeckOrderBinding.TryResolve(0, new[] { A, A }, out _, out var error));
        Assert.Equal("duplicate instance id in saved order", error);
    }
}

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
