using BlindSpire.Core.Reveal;
using Xunit;

namespace BlindSpire.Tests;

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
    public void Hover_UnknownShowsFrameButNotFace()
    {
        Assert.True(RevealRules.ShowsFrameOnHover(CardKnowledge.Unknown));
        Assert.False(RevealRules.ShowsFaceOnHover(CardKnowledge.Unknown));
        Assert.True(RevealRules.ShowsFrameOnHover(CardKnowledge.Revealed));
        Assert.True(RevealRules.ShowsFaceOnHover(CardKnowledge.Revealed));
    }

    [Fact]
    public void UpgradeMarker_OnlyWhenUpgraded()
    {
        Assert.True(RevealRules.ShowsUpgradeMarker(true));
        Assert.False(RevealRules.ShowsUpgradeMarker(false));
    }
}
