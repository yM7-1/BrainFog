using BrainFog.Core.Text;
using Xunit;

namespace BrainFog.Tests;

public class GlobalTextBlurRulesTests
{
    [Fact]
    public void NoAncestors_IsBlurred()
    {
        Assert.True(GlobalTextBlurRules.ShouldBlur(Array.Empty<string>(), modOwned: false));
    }

    [Theory]
    [InlineData("NTopBarHp")]
    [InlineData("NTopBarGold")]
    [InlineData("NMapScreen")]
    [InlineData("NMapLegendItem")]
    [InlineData("NMapPoint")]
    public void TopBarAndMap_AreBlurred(string ancestor)
    {
        // The unified ratio covers HP/gold values and map UI too (2026-09-21).
        Assert.True(GlobalTextBlurRules.ShouldBlur(new[] { ancestor }, modOwned: false));
    }

    [Theory]
    [InlineData("NCard")]
    [InlineData("NHoverTipSet")]
    [InlineData("NCardLibrary")]
    [InlineData("NIntent")]
    [InlineData("NSettingsScreen")]
    [InlineData("NMainMenuTextButton")]
    [InlineData("NPauseMenuButton")]
    [InlineData("NMerchantDialogue")]
    [InlineData("NAncientDialogueLine")]
    public void OwnedContexts_AreSkipped(string ancestor)
    {
        Assert.False(GlobalTextBlurRules.ShouldBlur(new[] { ancestor }, modOwned: false));
    }

    [Theory]
    [InlineData("NDamageNumVfx")]
    [InlineData("NHealNumVfx")]
    [InlineData("NDamageBlockedVfx")]
    public void CombatNumberVfx_AreBlurred(string ancestor)
    {
        Assert.True(GlobalTextBlurRules.ShouldBlur(new[] { ancestor }, modOwned: false));
    }

    [Theory]
    [InlineData("NFullscreenTextVfx")]
    [InlineData("NSpeechBubbleVfx")]
    [InlineData("NGainEpochVfx")]
    public void OtherVfx_StayReadable(string ancestor)
    {
        Assert.False(GlobalTextBlurRules.ShouldBlur(new[] { ancestor }, modOwned: false));
    }

    [Fact]
    public void ModOwned_IsSkipped()
    {
        Assert.False(GlobalTextBlurRules.ShouldBlur(new[] { "NTopBarHp" }, modOwned: true));
    }

    [Fact]
    public void NearestAncestorWins()
    {
        Assert.False(GlobalTextBlurRules.ShouldBlur(new[] { "NCard", "NTopBarHp" }, modOwned: false));
        Assert.True(GlobalTextBlurRules.ShouldBlur(new[] { "NDamageNumVfx", "NCombatRoom" }, modOwned: false));
    }
}
