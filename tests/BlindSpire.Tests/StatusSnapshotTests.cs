using BlindSpire.Core.Status;
using Xunit;

namespace BlindSpire.Tests;

public class StatusSnapshotTests
{
    [Fact]
    public void Init_SetsAllValues()
    {
        var snapshot = new StatusSnapshot();
        snapshot.Init(hp: 30, maxHp: 80, gold: 120);
        Assert.Equal(30, snapshot.Hp);
        Assert.Equal(80, snapshot.MaxHp);
        Assert.Equal(120, snapshot.Gold);
        Assert.True(snapshot.IsInitialized);
    }

    [Fact]
    public void Gold_RefreshesOnlyOnDeduction()
    {
        var snapshot = new StatusSnapshot();
        snapshot.Init(30, 80, 120);
        Assert.True(snapshot.ShouldRefreshGold(90));
        Assert.False(snapshot.ShouldRefreshGold(150));
        Assert.False(snapshot.ShouldRefreshGold(120));
    }

    [Fact]
    public void RefreshGold_UpdatesShownValue()
    {
        var snapshot = new StatusSnapshot();
        snapshot.Init(30, 80, 120);
        snapshot.RefreshGold(90);
        Assert.Equal(90, snapshot.Gold);
        Assert.False(snapshot.ShouldRefreshGold(100));
    }

    [Fact]
    public void RefreshHp_UpdatesShownValue()
    {
        var snapshot = new StatusSnapshot();
        snapshot.Init(30, 80, 120);
        snapshot.RefreshHp(55, 80);
        Assert.Equal(55, snapshot.Hp);
        Assert.Equal(80, snapshot.MaxHp);
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(0, 1, false)]
    [InlineData(100, 100, false)]
    [InlineData(100, 0, true)]
    [InlineData(1000000, 999999, true)]
    [InlineData(999999, 1000000, false)]
    public void Gold_DeductionEdgeCases(int shown, int next, bool expected)
    {
        var snapshot = new StatusSnapshot();
        snapshot.Init(30, 80, shown);
        Assert.Equal(expected, snapshot.ShouldRefreshGold(next));
    }

    [Fact]
    public void Uninitialized_DoesNotRefreshGold()
    {
        var snapshot = new StatusSnapshot();
        Assert.False(snapshot.IsInitialized);
        Assert.False(snapshot.ShouldRefreshGold(10));
    }

    [Fact]
    public void ReInit_ResetsToNewRunValues()
    {
        var snapshot = new StatusSnapshot();
        snapshot.Init(30, 80, 120);
        snapshot.RefreshHp(10, 80);
        snapshot.Init(70, 90, 5);
        Assert.Equal(70, snapshot.Hp);
        Assert.Equal(90, snapshot.MaxHp);
        Assert.Equal(5, snapshot.Gold);
    }
}
