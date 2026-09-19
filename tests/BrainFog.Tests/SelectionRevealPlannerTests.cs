using BrainFog.Core.Options;
using Xunit;

namespace BrainFog.Tests;

public class SelectionRevealPlannerTests
{
    private static readonly string[] Three = { "cards.STRIKE", "cards.BASH", "cards.CLEAVE" };
    private static readonly string[] Four = { "cards.A", "cards.B", "cards.C", "cards.D" };

    [Fact]
    public void None_RevealsNothing()
    {
        Assert.Empty(SelectionRevealPlanner.ResolveRevealedIndexes(Three, SelectionRevealOption.None, 42));
    }

    [Fact]
    public void All_RevealsEveryIndex()
    {
        var revealed = SelectionRevealPlanner.ResolveRevealedIndexes(Three, SelectionRevealOption.All, 42);
        Assert.Equal(new[] { 0, 1, 2 }, revealed);
    }

    [Fact]
    public void RandomOne_RevealsExactlyOne()
    {
        var revealed = SelectionRevealPlanner.ResolveRevealedIndexes(Three, SelectionRevealOption.RandomOne, 42);
        Assert.Single(revealed);
    }

    [Fact]
    public void RandomTwo_RevealsExactlyTwoOfThree()
    {
        var revealed = SelectionRevealPlanner.ResolveRevealedIndexes(Three, SelectionRevealOption.RandomTwo, 42);
        Assert.Equal(2, revealed.Count);
        Assert.All(revealed, index => Assert.InRange(index, 0, 2));
    }

    [Fact]
    public void RandomThree_OnThreeCards_RevealsAll()
    {
        var revealed = SelectionRevealPlanner.ResolveRevealedIndexes(Three, SelectionRevealOption.RandomThree, 42);
        Assert.Equal(new[] { 0, 1, 2 }, revealed);
    }

    [Fact]
    public void Pick_IsDeterministicForSameOfferAndSalt()
    {
        var first = SelectionRevealPlanner.ResolveRevealedIndexes(Four, SelectionRevealOption.RandomTwo, 7);
        var second = SelectionRevealPlanner.ResolveRevealedIndexes(Four, SelectionRevealOption.RandomTwo, 7);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Salt_ChangesThePick()
    {
        var seen = new HashSet<string>();
        for (var salt = 0; salt < 32; salt++)
        {
            var revealed = SelectionRevealPlanner.ResolveRevealedIndexes(Four, SelectionRevealOption.RandomOne, salt);
            seen.Add(string.Join(",", revealed));
        }
        Assert.True(seen.Count > 1, "the salt should vary the revealed slot");
    }

    [Fact]
    public void IsRevealed_MatchesResolve()
    {
        const int salt = 123;
        var expected = SelectionRevealPlanner.ResolveRevealedIndexes(Four, SelectionRevealOption.RandomTwo, salt);
        for (var index = 0; index < Four.Length; index++)
        {
            Assert.Equal(
                expected.Contains(index),
                SelectionRevealPlanner.IsRevealed(Four, SelectionRevealOption.RandomTwo, salt, index));
        }
    }

    [Fact]
    public void EmptyOffer_RevealsNothing()
    {
        Assert.Empty(SelectionRevealPlanner.ResolveRevealedIndexes(Array.Empty<string>(), SelectionRevealOption.All, 1));
        Assert.Empty(SelectionRevealPlanner.ResolveRevealedIndexes(Array.Empty<string>(), SelectionRevealOption.RandomTwo, 1));
    }
}
