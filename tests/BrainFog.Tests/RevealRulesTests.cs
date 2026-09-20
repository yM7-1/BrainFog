using BrainFog.Core.Options;
using BrainFog.Core.Reveal;
using Xunit;

namespace BrainFog.Tests;

public class RevealRulesTests
{
    [Theory]
    [InlineData(CardDisplayContext.Hand)]
    [InlineData(CardDisplayContext.DeckView)]
    [InlineData(CardDisplayContext.DrawPile)]
    [InlineData(CardDisplayContext.DiscardPile)]
    [InlineData(CardDisplayContext.ExhaustPile)]
    [InlineData(CardDisplayContext.PileView)]
    [InlineData(CardDisplayContext.Other)]
    public void OwnedContexts_UnknownIsFog_RevealedIsFullFace(CardDisplayContext context)
    {
        Assert.Equal(CardVisualRule.BlackFog, RevealRules.Resolve(context, CardKnowledge.Unknown));
        Assert.Equal(CardVisualRule.FullFace, RevealRules.Resolve(context, CardKnowledge.Revealed));
    }

    [Theory]
    [InlineData(CardDisplayContext.Reward)]
    [InlineData(CardDisplayContext.Shop)]
    [InlineData(CardDisplayContext.EventAcquisition)]
    public void AcquisitionContexts_AlwaysRarityOnly(CardDisplayContext context)
    {
        Assert.True(RevealRules.IsAcquisition(context));
        Assert.Equal(CardVisualRule.RarityOnly, RevealRules.Resolve(context, CardKnowledge.Unknown));
        Assert.Equal(CardVisualRule.RarityOnly, RevealRules.Resolve(context, CardKnowledge.Revealed));
    }

    [Fact]
    public void CardLibrary_IsNeverFogged()
    {
        Assert.Equal(CardVisualRule.FullFace, RevealRules.Resolve(CardDisplayContext.CardLibrary, CardKnowledge.Unknown));
        Assert.Equal(CardVisualRule.FullFace, RevealRules.Resolve(CardDisplayContext.CardLibrary, CardKnowledge.Revealed));
    }

    [Fact]
    public void UpgradeMarker_OnlyWhenUpgraded()
    {
        Assert.True(RevealRules.ShowsUpgradeMarker(true));
        Assert.False(RevealRules.ShowsUpgradeMarker(false));
    }

    [Fact]
    public void Reward_SelectionSlotRevealed_ShowsFace()
    {
        var settings = new DifficultySettings { SelectionReveal = SelectionRevealOption.RandomOne };
        Assert.Equal(
            CardVisualRule.FullFace,
            RevealRules.Resolve(CardDisplayContext.Reward, CardKnowledge.Unknown, settings, selectionSlotRevealed: true));
        Assert.Equal(
            CardVisualRule.RarityOnly,
            RevealRules.Resolve(CardDisplayContext.Reward, CardKnowledge.Unknown, settings, selectionSlotRevealed: false));
    }

    [Fact]
    public void Reward_WithoutSettings_StaysRarityOnlyEvenWhenMarked()
    {
        Assert.Equal(
            CardVisualRule.RarityOnly,
            RevealRules.Resolve(CardDisplayContext.Reward, CardKnowledge.Unknown, null, selectionSlotRevealed: true));
    }

    [Fact]
    public void ShopAndEvent_RevealToggle_ControlsFace()
    {
        var withReveal = new DifficultySettings { RevealShopAndEventCards = true };
        Assert.Equal(
            CardVisualRule.FullFace,
            RevealRules.Resolve(CardDisplayContext.Shop, CardKnowledge.Unknown, withReveal));
        Assert.Equal(
            CardVisualRule.FullFace,
            RevealRules.Resolve(CardDisplayContext.EventAcquisition, CardKnowledge.Unknown, withReveal));

        var withoutReveal = new DifficultySettings { RevealShopAndEventCards = false };
        Assert.Equal(
            CardVisualRule.RarityOnly,
            RevealRules.Resolve(CardDisplayContext.Shop, CardKnowledge.Unknown, withoutReveal));
        Assert.Equal(
            CardVisualRule.RarityOnly,
            RevealRules.Resolve(CardDisplayContext.EventAcquisition, CardKnowledge.Unknown, withoutReveal));
    }

    [Fact]
    public void Options_DoNotChangeOwnedContexts()
    {
        var revealAll = new DifficultySettings
        {
            SelectionReveal = SelectionRevealOption.All,
            RevealShopAndEventCards = true,
        };
        Assert.Equal(
            CardVisualRule.BlackFog,
            RevealRules.Resolve(CardDisplayContext.Hand, CardKnowledge.Unknown, revealAll, selectionSlotRevealed: true));
        Assert.Equal(
            CardVisualRule.FullFace,
            RevealRules.Resolve(CardDisplayContext.Hand, CardKnowledge.Revealed, revealAll, selectionSlotRevealed: true));
    }

    [Fact]
    public void Omniscient_RevealsEveryContext()
    {
        var settings = new DifficultySettings { MemoryMode = CardMemoryMode.Omniscient };
        foreach (var context in Enum.GetValues<CardDisplayContext>())
        {
            Assert.Equal(
                CardVisualRule.FullFace,
                RevealRules.Resolve(context, CardKnowledge.Unknown, settings));
        }
    }

    [Fact]
    public void Nonsense_NeverReveals()
    {
        var settings = new DifficultySettings
        {
            MemoryMode = CardMemoryMode.Nonsense,
            RevealShopAndEventCards = true,
        };
        Assert.Equal(
            CardVisualRule.BlackFog,
            RevealRules.Resolve(CardDisplayContext.Hand, CardKnowledge.Revealed, settings));
        // Acquisition keeps the rarity border but never the face, even with the
        // reveal toggles on (mode overrides them).
        Assert.Equal(
            CardVisualRule.RarityOnly,
            RevealRules.Resolve(CardDisplayContext.Reward, CardKnowledge.Revealed, settings, selectionSlotRevealed: true));
        Assert.Equal(
            CardVisualRule.RarityOnly,
            RevealRules.Resolve(CardDisplayContext.Shop, CardKnowledge.Revealed, settings));
        // The compendium stays readable.
        Assert.Equal(
            CardVisualRule.FullFace,
            RevealRules.Resolve(CardDisplayContext.CardLibrary, CardKnowledge.Unknown, settings));
    }

    [Fact]
    public void GoodMemory_KnownCardShowsFaceInAcquisition()
    {
        var settings = new DifficultySettings { MemoryMode = CardMemoryMode.GoodMemory };
        Assert.Equal(
            CardVisualRule.FullFace,
            RevealRules.Resolve(CardDisplayContext.Reward, CardKnowledge.Revealed, settings));
        Assert.Equal(
            CardVisualRule.FullFace,
            RevealRules.Resolve(CardDisplayContext.Shop, CardKnowledge.Revealed, settings));
        Assert.Equal(
            CardVisualRule.FullFace,
            RevealRules.Resolve(CardDisplayContext.EventAcquisition, CardKnowledge.Revealed, settings));
        // Unknown cards still follow the acquisition settings.
        Assert.Equal(
            CardVisualRule.RarityOnly,
            RevealRules.Resolve(CardDisplayContext.Reward, CardKnowledge.Unknown, settings));
    }

    [Fact]
    public void BadMemory_DoesNotOverrideAcquisitionSettings()
    {
        var settings = new DifficultySettings
        {
            MemoryMode = CardMemoryMode.BadMemory,
            RevealShopAndEventCards = true,
        };
        Assert.Equal(
            CardVisualRule.FullFace,
            RevealRules.Resolve(CardDisplayContext.Shop, CardKnowledge.Revealed, settings));
        Assert.Equal(
            CardVisualRule.BlackFog,
            RevealRules.Resolve(CardDisplayContext.Hand, CardKnowledge.Unknown, settings));
    }
}
