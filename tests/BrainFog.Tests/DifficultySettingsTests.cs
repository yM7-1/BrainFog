using BrainFog.Core.Options;
using Xunit;

namespace BrainFog.Tests;

public class DifficultySettingsTests
{
    [Fact]
    public void Defaults_MatchTheDocumentedState()
    {
        var settings = new DifficultySettings();
        Assert.Equal(SelectionRevealOption.None, settings.SelectionReveal);
        Assert.False(settings.RevealShopAndEventCards);
        Assert.Equal(CardMemoryMode.BadMemory, settings.MemoryMode);
        Assert.Equal(1, settings.BadMemoryThreshold);
        Assert.False(settings.SnapshotStatus);
        Assert.Equal(50, settings.TextBlurPercent);
        Assert.Equal(BlurSaltMode.PerLaunch, settings.SaltMode);
        Assert.False(settings.ShowOwnedRelics);
        Assert.False(settings.ShowAllMapRoutes);
        Assert.Equal(IntentVisibility.Hidden, settings.IntentMode);
    }

    [Fact]
    public void ApplyDefaults_RestoresEveryOption()
    {
        var settings = new DifficultySettings
        {
            SelectionReveal = SelectionRevealOption.All,
            RevealShopAndEventCards = true,
            MemoryMode = CardMemoryMode.Omniscient,
            BadMemoryThreshold = 7,
            SnapshotStatus = true,
            TextBlurPercent = 99,
            SaltMode = BlurSaltMode.Fixed,
            ShowOwnedRelics = true,
            ShowAllMapRoutes = true,
            IntentMode = IntentVisibility.All,
        };

        settings.ApplyDefaults();

        Assert.Equal(SelectionRevealOption.None, settings.SelectionReveal);
        Assert.False(settings.RevealShopAndEventCards);
        Assert.Equal(CardMemoryMode.BadMemory, settings.MemoryMode);
        Assert.Equal(1, settings.BadMemoryThreshold);
        Assert.False(settings.SnapshotStatus);
        Assert.Equal(50, settings.TextBlurPercent);
        Assert.Equal(BlurSaltMode.PerLaunch, settings.SaltMode);
        Assert.False(settings.ShowOwnedRelics);
        Assert.False(settings.ShowAllMapRoutes);
        Assert.Equal(IntentVisibility.Hidden, settings.IntentMode);
    }

    [Theory]
    [InlineData(CardMemoryMode.Omniscient, "omniscient")]
    [InlineData(CardMemoryMode.GoodMemory, "good")]
    [InlineData(CardMemoryMode.BadMemory, "bad")]
    [InlineData(CardMemoryMode.Nonsense, "nonsense")]
    public void MemoryMode_StorageRoundTrips(CardMemoryMode mode, string stored)
    {
        Assert.Equal(stored, CardMemoryModeRules.ToStorage(mode));
        Assert.Equal(mode, CardMemoryModeRules.Parse(stored));
        Assert.Equal(mode, CardMemoryModeRules.FromIndex(CardMemoryModeRules.ToIndex(mode)));
    }

    [Theory]
    [InlineData(IntentVisibility.Hidden, "hidden")]
    [InlineData(IntentVisibility.FirstRoundOnly, "first")]
    [InlineData(IntentVisibility.All, "all")]
    public void IntentVisibility_StorageRoundTrips(IntentVisibility mode, string stored)
    {
        Assert.Equal(stored, IntentVisibilityRules.ToStorage(mode));
        Assert.Equal(mode, IntentVisibilityRules.Parse(stored));
        Assert.Equal(mode, IntentVisibilityRules.FromIndex(IntentVisibilityRules.ToIndex(mode)));
    }

    [Theory]
    [InlineData(IntentVisibility.Hidden, true, false)]
    [InlineData(IntentVisibility.Hidden, false, false)]
    [InlineData(IntentVisibility.FirstRoundOnly, true, true)]
    [InlineData(IntentVisibility.FirstRoundOnly, false, false)]
    [InlineData(IntentVisibility.All, true, true)]
    [InlineData(IntentVisibility.All, false, true)]
    public void IntentVisibility_CombinesWithFirstRoundGate(
        IntentVisibility mode, bool gate, bool expected)
    {
        Assert.Equal(expected, IntentVisibilityRules.ShouldShow(mode, gate));
    }

    [Theory]
    [InlineData(BlurSaltMode.Fixed, "fixed")]
    [InlineData(BlurSaltMode.PerLaunch, "per-launch")]
    public void BlurSaltMode_StorageRoundTrips(BlurSaltMode mode, string stored)
    {
        Assert.Equal(stored, BlurSaltModeRules.ToStorage(mode));
        Assert.Equal(mode, BlurSaltModeRules.Parse(stored));
        Assert.Equal(mode, BlurSaltModeRules.FromIndex(BlurSaltModeRules.ToIndex(mode)));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(200, 99)]
    public void BadMemoryThreshold_IsClamped(int input, int expected)
    {
        Assert.Equal(expected, DifficultySettings.ClampBadMemoryThreshold(input));
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
