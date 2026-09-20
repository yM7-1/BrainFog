using BrainFog.Core.Text;
using Xunit;

namespace BrainFog.Tests;

public class GlobalTextBlurRulesTests
{
    [Fact]
    public void NoAncestors_DefaultsToSixty()
    {
        Assert.Equal(TextBlurPercents.Default, GlobalTextBlurRules.ResolvePercent(Array.Empty<string>(), false, false));
    }

    [Theory]
    [InlineData("NTopBarHp")]
    [InlineData("NTopBarGold")]
    [InlineData("NMapScreen")]
    [InlineData("NMapLegendItem")]
    [InlineData("NMapPoint")]
    public void TopBarAndMap_UseSeventy(string ancestor)
    {
        Assert.Equal(TextBlurPercents.UiDescription, GlobalTextBlurRules.ResolvePercent(new[] { ancestor }, false, false));
    }

    [Theory]
    [InlineData("NCard")]
    [InlineData("NHoverTipSet")]
    [InlineData("NCardLibrary")]
    [InlineData("NIntent")]
    [InlineData("NSettingsScreen")]
    [InlineData("NEventLayout")]
    [InlineData("NEventOptionButton")]
    [InlineData("NMainMenuTextButton")]
    [InlineData("NPauseMenuButton")]
    [InlineData("NMerchantDialogue")]
    [InlineData("NAncientDialogueLine")]
    public void OwnedContexts_AreSkipped(string ancestor)
    {
        Assert.Null(GlobalTextBlurRules.ResolvePercent(new[] { ancestor }, false, false));
    }

    [Theory]
    [InlineData("NDamageNumVfx")]
    [InlineData("NHealNumVfx")]
    [InlineData("NDamageBlockedVfx")]
    public void CombatNumberVfx_AreGarbled(string ancestor)
    {
        Assert.Equal(TextBlurPercents.Default, GlobalTextBlurRules.ResolvePercent(new[] { ancestor }, false, false));
    }

    [Theory]
    [InlineData("NFullscreenTextVfx")]
    [InlineData("NSpeechBubbleVfx")]
    [InlineData("NGainEpochVfx")]
    public void OtherVfx_StaysReadable(string ancestor)
    {
        Assert.Null(GlobalTextBlurRules.ResolvePercent(new[] { ancestor }, false, false));
    }

    [Fact]
    public void ModOwnedAndTopBarValues_AreSkipped()
    {
        Assert.Null(GlobalTextBlurRules.ResolvePercent(new[] { "NTopBarHp" }, modOwned: true, topBarValueLabel: false));
        Assert.Null(GlobalTextBlurRules.ResolvePercent(new[] { "NTopBarHp" }, modOwned: false, topBarValueLabel: true));
    }

    [Fact]
    public void NearestAncestorWins()
    {
        // A card label inside the top bar keeps its own (skip) rule.
        Assert.Null(GlobalTextBlurRules.ResolvePercent(new[] { "NCard", "NTopBarHp" }, false, false));
        Assert.Equal(
            TextBlurPercents.Default,
            GlobalTextBlurRules.ResolvePercent(new[] { "NDamageNumVfx", "NCombatRoom" }, false, false));
    }
}
