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

    [Fact]
    public void Load_NullInput_ClearsAndDoesNotThrow()
    {
        var tracker = new CardRevealTracker();
        tracker.RevealByPlay("BlindSpire.a");
        tracker.Load(null);
        Assert.Equal(0, tracker.RevealedCount);
    }

    [Fact]
    public void Load_Reset_Load_RoundTrips()
    {
        var tracker = new CardRevealTracker();
        tracker.Load(new[] { "BlindSpire.a", "BlindSpire.b" });
        tracker.Reset();
        Assert.Equal(0, tracker.RevealedCount);
        tracker.Load(new[] { "BlindSpire.b", "BlindSpire.c" });
        Assert.False(tracker.IsRevealed("BlindSpire.a"));
        Assert.True(tracker.IsRevealed("BlindSpire.b"));
        Assert.True(tracker.IsRevealed("BlindSpire.c"));
    }

    [Fact]
    public void RevealedIds_IsADefensiveCopy()
    {
        var tracker = new CardRevealTracker();
        tracker.RevealByPlay("BlindSpire.a");
        var ids = (ICollection<string>)tracker.RevealedIds;
        Assert.Throws<NotSupportedException>(() => ids.Add("BlindSpire.hack"));
        Assert.Equal(1, tracker.RevealedCount);
    }

    [Fact]
    public void CrossReveal_PathsAgree()
    {
        var tracker = new CardRevealTracker();
        Assert.True(tracker.RevealByPlay("BlindSpire.a"));
        Assert.False(tracker.RevealByUpgrade("BlindSpire.a"));
        Assert.True(tracker.RevealByUpgrade("BlindSpire.b"));
        Assert.False(tracker.RevealByPlay("BlindSpire.b"));
    }
}
