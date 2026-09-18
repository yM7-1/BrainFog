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
}
