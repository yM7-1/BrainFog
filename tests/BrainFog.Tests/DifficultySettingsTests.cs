using BrainFog.Core.Options;
using Xunit;

namespace BrainFog.Tests;

public class DifficultySettingsTests
{
    [Fact]
    public void Defaults_HardestVariant()
    {
        var settings = new DifficultySettings();
        Assert.Equal(SelectionRevealOption.None, settings.SelectionReveal);
        Assert.False(settings.RevealShopAndEventCards);
        Assert.True(settings.RevealSameNameCards);
        Assert.False(settings.ShowLiveStatus);
        Assert.False(settings.ShowOwnedRelics);
        Assert.False(settings.ShowAllMapRoutes);
    }

    [Theory]
    [InlineData(SelectionRevealOption.None, "none")]
    [InlineData(SelectionRevealOption.RandomOne, "1")]
    [InlineData(SelectionRevealOption.RandomTwo, "2")]
    [InlineData(SelectionRevealOption.RandomThree, "3")]
    [InlineData(SelectionRevealOption.All, "all")]
    public void Storage_RoundTrips(SelectionRevealOption option, string stored)
    {
        Assert.Equal(stored, DifficultySettings.ToStorage(option));
        Assert.Equal(option, DifficultySettings.ParseSelectionReveal(stored));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bogus")]
    [InlineData("99")]
    public void Parse_UnknownFallsBackToNone(string? input)
    {
        Assert.Equal(SelectionRevealOption.None, DifficultySettings.ParseSelectionReveal(input));
    }

    [Fact]
    public void IndexConversion_RoundTrips()
    {
        for (var index = 0; index <= 4; index++)
        {
            Assert.Equal(index, DifficultySettings.ToIndex(DifficultySettings.FromIndex(index)));
        }
    }
}
