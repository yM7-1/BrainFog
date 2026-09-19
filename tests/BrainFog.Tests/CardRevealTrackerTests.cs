using BrainFog.Core.Reveal;
using Xunit;

namespace BrainFog.Tests;

public class CardRevealTrackerTests
{
    [Fact]
    public void NewTracker_ReportsUnknown()
    {
        var tracker = new CardRevealTracker();
        Assert.Equal(CardKnowledge.Unknown, tracker.GetKnowledge("cards.STRIKE"));
        Assert.False(tracker.IsRevealed("cards.STRIKE"));
        Assert.Equal(0, tracker.RevealedCount);
    }

    [Fact]
    public void RevealByPlay_MarksDefinition()
    {
        var tracker = new CardRevealTracker();
        Assert.True(tracker.RevealByPlay("cards.STRIKE"));
        Assert.True(tracker.IsRevealed("cards.STRIKE"));
        Assert.False(tracker.RevealByPlay("cards.STRIKE"));
    }

    [Fact]
    public void RevealByUpgrade_MarksDefinitionEvenIfNeverPlayed()
    {
        var tracker = new CardRevealTracker();
        Assert.True(tracker.RevealByUpgrade("cards.BASH"));
        Assert.Equal(CardKnowledge.Revealed, tracker.GetKnowledge("cards.BASH"));
    }

    [Fact]
    public void Reveal_IsPerDefinition_OtherCardsStayUnknown()
    {
        var tracker = new CardRevealTracker();
        tracker.RevealByPlay("cards.STRIKE");
        Assert.True(tracker.IsRevealed("cards.STRIKE"));
        Assert.False(tracker.IsRevealed("cards.DEFEND"));
    }

    [Fact]
    public void Reset_ClearsEverything()
    {
        var tracker = new CardRevealTracker();
        tracker.RevealByPlay("cards.STRIKE");
        tracker.RevealByUpgrade("cards.BASH");
        tracker.Reset();
        Assert.Equal(0, tracker.RevealedCount);
        Assert.False(tracker.IsRevealed("cards.STRIKE"));
    }

    [Fact]
    public void Load_RestoresKeysAndIgnoresEmptyOnes()
    {
        var tracker = new CardRevealTracker();
        tracker.Load(new[] { "cards.STRIKE", "", "  ", "cards.BASH", null! });
        Assert.True(tracker.IsRevealed("cards.STRIKE"));
        Assert.True(tracker.IsRevealed("cards.BASH"));
        Assert.Equal(2, tracker.RevealedCount);
    }

    [Fact]
    public void Load_NullInput_ClearsAndDoesNotThrow()
    {
        var tracker = new CardRevealTracker();
        tracker.RevealByPlay("cards.STRIKE");
        tracker.Load(null);
        Assert.Equal(0, tracker.RevealedCount);
    }

    [Fact]
    public void Load_Reset_Load_RoundTrips()
    {
        var tracker = new CardRevealTracker();
        tracker.Load(new[] { "cards.STRIKE", "cards.BASH" });
        tracker.Reset();
        Assert.Equal(0, tracker.RevealedCount);
        tracker.Load(new[] { "cards.BASH", "cards.CLEAVE" });
        Assert.False(tracker.IsRevealed("cards.STRIKE"));
        Assert.True(tracker.IsRevealed("cards.BASH"));
        Assert.True(tracker.IsRevealed("cards.CLEAVE"));
    }

    [Fact]
    public void RevealedKeys_IsADefensiveCopy()
    {
        var tracker = new CardRevealTracker();
        tracker.RevealByPlay("cards.STRIKE");
        var keys = (ICollection<string>)tracker.RevealedKeys;
        Assert.Throws<NotSupportedException>(() => keys.Add("cards.HACK"));
        Assert.Equal(1, tracker.RevealedCount);
    }

    [Fact]
    public void CrossReveal_PathsAgree()
    {
        var tracker = new CardRevealTracker();
        Assert.True(tracker.RevealByPlay("cards.STRIKE"));
        Assert.False(tracker.RevealByUpgrade("cards.STRIKE"));
        Assert.True(tracker.RevealByUpgrade("cards.BASH"));
        Assert.False(tracker.RevealByPlay("cards.BASH"));
    }
}
