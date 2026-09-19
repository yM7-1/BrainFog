using BrainFog.Core.Status;
using Xunit;

namespace BrainFog.Tests;

public class LowHpHintTests
{
    [Theory]
    [InlineData(14, 100, true)]
    [InlineData(15, 100, false)]
    [InlineData(1, 80, true)]
    [InlineData(11, 80, true)]
    [InlineData(12, 80, false)]
    [InlineData(79, 80, false)]
    public void IsLow_Respects15PercentThreshold(int hp, int maxHp, bool expected)
    {
        Assert.Equal(expected, LowHpHint.IsLow(hp, maxHp));
    }

    [Fact]
    public void IsLow_FalseForZeroOrInvalidValues()
    {
        Assert.False(LowHpHint.IsLow(0, 80));
        Assert.False(LowHpHint.IsLow(5, 0));
        Assert.False(LowHpHint.IsLow(-1, 80));
        Assert.False(LowHpHint.IsLow(80, 80));
        Assert.False(LowHpHint.IsLow(5, -80));
    }

    [Fact]
    public void Message_IsPresent()
    {
        Assert.False(string.IsNullOrWhiteSpace(LowHpHint.Message));
    }
}
