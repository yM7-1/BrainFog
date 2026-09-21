using BrainFog.Core.Options;
using BrainFog.Core.Reveal;
using Xunit;

namespace BrainFog.Tests;

public class MemoryFadeRulesTests
{
    [Theory]
    [InlineData(CardMemoryMode.GoodMemory, true)]
    [InlineData(CardMemoryMode.BadMemory, true)]
    [InlineData(CardMemoryMode.Omniscient, false)]
    [InlineData(CardMemoryMode.Nonsense, false)]
    public void Applies_OnlyForGoodAndBadMemory(CardMemoryMode mode, bool expected)
    {
        Assert.Equal(expected, MemoryFadeRules.Applies(mode));
    }

    [Theory]
    // good memory looks at the definition reveal
    [InlineData(CardMemoryMode.GoodMemory, true, false, false)]
    [InlineData(CardMemoryMode.GoodMemory, false, true, true)]
    // bad memory looks at the copy reveal
    [InlineData(CardMemoryMode.BadMemory, true, false, true)]
    [InlineData(CardMemoryMode.BadMemory, false, true, false)]
    [InlineData(CardMemoryMode.BadMemory, false, false, true)]
    // exempt modes never count as forgotten
    [InlineData(CardMemoryMode.Nonsense, false, false, false)]
    [InlineData(CardMemoryMode.Omniscient, false, false, false)]
    public void IsUnrevealed_UsesTheModeScope(
        CardMemoryMode mode, bool definitionRevealed, bool instanceRevealed, bool expected)
    {
        Assert.Equal(expected, MemoryFadeRules.IsUnrevealed(mode, definitionRevealed, instanceRevealed));
    }
}
