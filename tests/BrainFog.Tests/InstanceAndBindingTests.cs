using BrainFog.Core.Reveal;
using Xunit;

namespace BrainFog.Tests;

public class InstanceIdsTests
{
    [Fact]
    public void NewId_HasPrefixAndIsValid()
    {
        var id = InstanceIds.NewId();
        Assert.StartsWith(InstanceIds.Prefix, id);
        Assert.True(InstanceIds.IsValid(id));
    }

    [Fact]
    public void NewId_IsUnique()
    {
        Assert.NotEqual(InstanceIds.NewId(), InstanceIds.NewId());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("BrainFog.")]
    [InlineData("Other.a")]
    public void IsValid_RejectsBadIds(string? id)
    {
        Assert.False(InstanceIds.IsValid(id));
    }
}

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

public class CardIdentityTests
{
    private sealed class Node
    {
        public Node? Parent;
    }

    private static Node Resolve(Node card) =>
        CardIdentity.Resolve(card, n => n.Parent);

    [Fact]
    public void Resolve_CombatClone_UsesDeckVersion()
    {
        var deckCard = new Node();
        var clone = new Node { Parent = deckCard };
        Assert.Same(deckCard, Resolve(clone));
    }

    [Fact]
    public void Resolve_NoParent_UsesItself()
    {
        var card = new Node();
        Assert.Same(card, Resolve(card));
    }

    [Fact]
    public void Resolve_StableAcrossClonesOfTheSameDeckCard()
    {
        var deckCard = new Node();
        Assert.Same(
            Resolve(new Node { Parent = deckCard }),
            Resolve(new Node { Parent = deckCard }));
    }

    [Fact]
    public void Resolve_FollowsChains()
    {
        var deckCard = new Node();
        var effectClone = new Node { Parent = new Node { Parent = deckCard } };
        Assert.Same(deckCard, Resolve(effectClone));
    }

    [Fact]
    public void Resolve_SelfCycle_Terminates()
    {
        var card = new Node();
        card.Parent = card;
        Assert.Same(card, Resolve(card));
    }

    [Fact]
    public void Resolve_LongCycle_TerminatesAtDepthLimit()
    {
        var a = new Node();
        var b = new Node { Parent = a };
        a.Parent = b; // 2-cycle
        var resolved = Resolve(b);
        Assert.True(resolved == a || resolved == b);
    }
}
