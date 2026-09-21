using BrainFog.Core.Options;
using Xunit;

namespace BrainFog.Tests;

public class DifficultySettingsTests
{
    [Fact]
    public void Defaults_MatchTheDocumentedState()
    {
        var settings = new DifficultySettings();
        Assert.Equal(SelectionRevealOption.All, settings.SelectionReveal);
        Assert.True(settings.RevealShopAndEventCards);
        Assert.Equal(CardMemoryMode.BadMemory, settings.MemoryMode);
        Assert.Equal(2, settings.BadMemoryThreshold);
        Assert.True(settings.MemoryFade);
        Assert.False(settings.SnapshotStatus);
        Assert.True(settings.ReadableStatusNumbers);
        Assert.Equal(50, settings.TextBlurPercent);
        Assert.Equal(BlurSaltMode.PerLaunch, settings.SaltMode);
        Assert.True(settings.ShowRelics);
        Assert.True(settings.ShowAllMapRoutes);
        Assert.Equal(IntentVisibility.All, settings.IntentMode);
        Assert.True(settings.EnemyModelsVisible);
    }

    [Fact]
    public void ApplyDefaults_RestoresEveryOption()
    {
        var settings = new DifficultySettings
        {
            SelectionReveal = SelectionRevealOption.None,
            RevealShopAndEventCards = false,
            MemoryMode = CardMemoryMode.Omniscient,
            BadMemoryThreshold = 7,
            MemoryFade = false,
            SnapshotStatus = true,
            ReadableStatusNumbers = false,
            TextBlurPercent = 99,
            SaltMode = BlurSaltMode.Fixed,
            ShowRelics = false,
            ShowAllMapRoutes = false,
            IntentMode = IntentVisibility.Hidden,
            EnemyModelsVisible = false,
        };

        settings.ApplyDefaults();

        Assert.Equal(SelectionRevealOption.All, settings.SelectionReveal);
        Assert.True(settings.RevealShopAndEventCards);
        Assert.Equal(CardMemoryMode.BadMemory, settings.MemoryMode);
        Assert.Equal(2, settings.BadMemoryThreshold);
        Assert.True(settings.MemoryFade);
        Assert.False(settings.SnapshotStatus);
        Assert.True(settings.ReadableStatusNumbers);
        Assert.Equal(50, settings.TextBlurPercent);
        Assert.Equal(BlurSaltMode.PerLaunch, settings.SaltMode);
        Assert.True(settings.ShowRelics);
        Assert.True(settings.ShowAllMapRoutes);
        Assert.Equal(IntentVisibility.All, settings.IntentMode);
        Assert.True(settings.EnemyModelsVisible);
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
    [Fact]
    public void CopyFrom_ClonesEveryOption()
    {
        var source = new DifficultySettings
        {
            SelectionReveal = SelectionRevealOption.RandomTwo,
            RevealShopAndEventCards = false,
            MemoryMode = CardMemoryMode.Nonsense,
            BadMemoryThreshold = 5,
            MemoryFade = false,
            SnapshotStatus = true,
            ReadableStatusNumbers = false,
            TextBlurPercent = 73,
            SaltMode = BlurSaltMode.Fixed,
            ShowRelics = false,
            ShowAllMapRoutes = false,
            IntentMode = IntentVisibility.FirstRoundOnly,
            EnemyModelsVisible = false,
        };
        var target = new DifficultySettings();

        target.CopyFrom(source);

        Assert.Equal(SelectionRevealOption.RandomTwo, target.SelectionReveal);
        Assert.False(target.RevealShopAndEventCards);
        Assert.Equal(CardMemoryMode.Nonsense, target.MemoryMode);
        Assert.Equal(5, target.BadMemoryThreshold);
        Assert.False(target.MemoryFade);
        Assert.True(target.SnapshotStatus);
        Assert.False(target.ReadableStatusNumbers);
        Assert.Equal(73, target.TextBlurPercent);
        Assert.Equal(BlurSaltMode.Fixed, target.SaltMode);
        Assert.False(target.ShowRelics);
        Assert.False(target.ShowAllMapRoutes);
        Assert.Equal(IntentVisibility.FirstRoundOnly, target.IntentMode);
        Assert.False(target.EnemyModelsVisible);

        source.TextBlurPercent = 1;
        Assert.Equal(73, target.TextBlurPercent); // independent copy
    }

}
