using BlindSpire.Core.Reveal;
using Xunit;

namespace BlindSpire.Tests;

public class CardRevealTrackerTests
{
    [Fact]
    public void NewTracker_ReportsUnknown()
    {
        var tracker = new CardRevealTracker();
        Assert.Equal(CardKnowledge.Unknown, tracker.GetKnowledge("BlindSpire.a"));
        Assert.False(tracker.IsRevealed("BlindSpire.a"));
        Assert.Equal(0, tracker.RevealedCount);
    }

    [Fact]
    public void RevealByPlay_MarksInstance()
    {
        var tracker = new CardRevealTracker();
        Assert.True(tracker.RevealByPlay("BlindSpire.a"));
        Assert.True(tracker.IsRevealed("BlindSpire.a"));
        Assert.False(tracker.RevealByPlay("BlindSpire.a"));
    }

    [Fact]
    public void RevealByUpgrade_MarksInstanceEvenIfNeverPlayed()
    {
        var tracker = new CardRevealTracker();
        Assert.True(tracker.RevealByUpgrade("BlindSpire.b"));
        Assert.Equal(CardKnowledge.Revealed, tracker.GetKnowledge("BlindSpire.b"));
    }

    [Fact]
    public void SameDefinitionCopies_AreTrackedSeparately()
    {
        var tracker = new CardRevealTracker();
        tracker.RevealByPlay("BlindSpire.first");
        Assert.True(tracker.IsRevealed("BlindSpire.first"));
        Assert.False(tracker.IsRevealed("BlindSpire.second"));
    }

    [Fact]
    public void Reset_ClearsEverything()
    {
        var tracker = new CardRevealTracker();
        tracker.RevealByPlay("BlindSpire.a");
        tracker.RevealByUpgrade("BlindSpire.b");
        tracker.Reset();
        Assert.Equal(0, tracker.RevealedCount);
        Assert.False(tracker.IsRevealed("BlindSpire.a"));
    }

    [Fact]
    public void Load_RestoresValidIdsAndIgnoresInvalidOnes()
    {
        var tracker = new CardRevealTracker();
        tracker.Load(new[] { "BlindSpire.a", "", "not-an-id", "BlindSpire.b", "BlindSpire." });
        Assert.True(tracker.IsRevealed("BlindSpire.a"));
        Assert.True(tracker.IsRevealed("BlindSpire.b"));
        Assert.Equal(2, tracker.RevealedCount);
    }
}
