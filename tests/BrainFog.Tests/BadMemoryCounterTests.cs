using BrainFog.Core.Reveal;
using Xunit;

namespace BrainFog.Tests;

public class BadMemoryCounterTests
{
    [Fact]
    public void UnplayedEntries_DecayAtThreshold()
    {
        var counter = new BadMemoryCounter();
        counter.OnEnterHand();
        Assert.False(counter.OnLeaveHand(2)); // 1 unplayed draw
        Assert.Equal(1, counter.UnplayedDraws);

        counter.OnEnterHand();
        Assert.True(counter.OnLeaveHand(2)); // reaches n = 2
        Assert.Equal(0, counter.UnplayedDraws); // reset after reverting
    }

    [Fact]
    public void PlayingResetsTheCounter()
    {
        var counter = new BadMemoryCounter();
        counter.OnEnterHand();
        Assert.False(counter.OnLeaveHand(2)); // count 1

        counter.OnEnterHand();
        counter.OnPlayed();
        Assert.False(counter.OnLeaveHand(2)); // played: no decay
        Assert.Equal(0, counter.UnplayedDraws);
    }

    [Fact]
    public void PlayedSinceEntry_SkipsTheLeaveCount()
    {
        var counter = new BadMemoryCounter();
        counter.OnEnterHand();
        counter.OnPlayed();
        Assert.False(counter.OnLeaveHand(1));
        Assert.Equal(0, counter.UnplayedDraws);
    }

    [Fact]
    public void ThresholdOne_DecaysOnFirstUnplayedLeave()
    {
        var counter = new BadMemoryCounter();
        counter.OnEnterHand();
        Assert.True(counter.OnLeaveHand(1));
    }

    [Fact]
    public void Restore_SeedsTheCounter()
    {
        var counter = new BadMemoryCounter();
        counter.Restore(3);
        Assert.Equal(3, counter.UnplayedDraws);
        Assert.True(counter.OnLeaveHand(4)); // 3 + 1 = 4
    }
}
