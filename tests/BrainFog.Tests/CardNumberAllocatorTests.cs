using System.Collections.Generic;
using BrainFog.Core.Reveal;
using Xunit;

namespace BrainFog.Tests;

public class CardNumberAllocatorTests
{
    private static readonly IReadOnlyDictionary<string, int> NoNumbers =
        new Dictionary<string, int>();

    [Fact]
    public void AssignMissing_NumbersSameNameCopiesInDeckOrder()
    {
        var next = new Dictionary<string, int>();
        var deck = new List<(string, string)>
        {
            ("id-a", "cards.STRIKE"),
            ("id-b", "cards.DEFEND"),
            ("id-c", "cards.STRIKE"),
            ("id-d", "cards.STRIKE"),
        };

        var assigned = CardNumberAllocator.AssignMissing(deck, NoNumbers, next);

        Assert.Equal(new[] { 1, 2, 3 }, new[] { assigned[0].Number, assigned[2].Number, assigned[3].Number });
        Assert.Equal("cards.STRIKE", assigned[0].DefinitionKey);
        Assert.Equal("id-b", assigned[1].Id);
        Assert.Equal(4, next["cards.STRIKE"]);
        Assert.Equal(2, next["cards.DEFEND"]);
    }

    [Fact]
    public void AssignMissing_SkipsCopiesThatAlreadyHaveANumber()
    {
        var next = new Dictionary<string, int> { ["cards.STRIKE"] = 3 };
        var assigned = new Dictionary<string, int> { ["id-a"] = 1, ["id-b"] = 2 };
        var deck = new List<(string, string)>
        {
            ("id-a", "cards.STRIKE"),
            ("id-b", "cards.STRIKE"),
            ("id-c", "cards.STRIKE"),
        };

        var result = CardNumberAllocator.AssignMissing(deck, assigned, next);

        var single = Assert.Single(result);
        Assert.Equal("id-c", single.Id);
        Assert.Equal(3, single.Number);
        Assert.Equal(4, next["cards.STRIKE"]);
    }

    [Fact]
    public void NextFor_ContinuesAfterRemovedCopies()
    {
        // A remembered "next" counter is not reset by removals: new copies
        // continue after the highest number ever assigned (user choice).
        var next = new Dictionary<string, int> { ["cards.STRIKE"] = 4 };
        Assert.Equal(4, CardNumberAllocator.NextFor("cards.STRIKE", next));
        Assert.Equal(5, next["cards.STRIKE"]);
        Assert.Equal(1, CardNumberAllocator.NextFor("cards.OTHER", next));
    }

    [Fact]
    public void FormatLabel_AppendsTheNumberOnlyWhenKnown()
    {
        Assert.Equal("打击1", CardNumberAllocator.FormatLabel("打击", 1));
        Assert.Equal("打击", CardNumberAllocator.FormatLabel("打击", 0));
    }
}
