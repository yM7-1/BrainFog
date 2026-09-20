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

    [Theory]
    [InlineData("HpLabel", "NTopBarHp")]
    [InlineData("GoldLabel", "NTopBarGold")]
    public void StatusNumberLabels_MatchTheirTopBarContext(string labelName, string ancestor)
    {
        // Panel option "readable HP/gold numbers" (2026-09-21): these two labels
        // are exempt from the catch-all blur while the option is on.
        Assert.True(GlobalTextBlurRules.IsStatusNumberName(labelName));
        Assert.True(GlobalTextBlurRules.IsStatusNumberLabel(new[] { ancestor }, labelName, false));
    }

    [Theory]
    [InlineData("HpLabel", "NTopBarGold")]
    [InlineData("GoldLabel", "NTopBarHp")]
    [InlineData("HpLabel", "NMapScreen")]
    [InlineData("BlockLabel", "NHealthBar")]
    [InlineData("HpLabel", "NHealthBar")]
    public void StatusNumberLabels_NeedTheirOwnContext(string labelName, string ancestor)
    {
        // No top-bar match and no local-player-bar flag: not a readable status number.
        Assert.False(GlobalTextBlurRules.IsStatusNumberLabel(new[] { ancestor }, labelName, false));
    }

    [Fact]
    public void StatusNumberLabels_LocalPlayerHealthBar()
    {
        // The combat bar number only counts for the local player (flag from the
        // runtime, which knows the creature).
        Assert.True(GlobalTextBlurRules.IsStatusNumberLabel(Array.Empty<string>(), "HpLabel", true));
        Assert.False(GlobalTextBlurRules.IsStatusNumberLabel(Array.Empty<string>(), "BlockLabel", true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("BlockLabel")]
    [InlineData("NameLabel")]
    public void StatusNumberName_RejectsOtherLabels(string? labelName)
    {
        Assert.False(GlobalTextBlurRules.IsStatusNumberName(labelName));
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
