using BrainFog.Core.Map;
using BrainFog.Core.Reveal;
using Xunit;

namespace BrainFog.Tests;

public class MapFogRulesTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public void IsFrontierNode_VisitedOrTravelable(bool traveled, bool travelable, bool expected)
    {
        Assert.Equal(expected, MapFogRules.IsFrontierNode(traveled, travelable));
    }

    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(false, true, true, false)]
    public void IsPathVisible_RequiresTraveledStartAndVisibleEndpoints(
        bool fromTraveled, bool fromVisible, bool toVisible, bool expected)
    {
        Assert.Equal(expected, MapFogRules.IsPathVisible(fromTraveled, fromVisible, toVisible));
    }
}

public class CardContextClassifierTests
{
    [Theory]
    [InlineData(new[] { "NCardHolder", "NCardLibrary" }, CardDisplayContext.CardLibrary)]
    [InlineData(new[] { "NCardHolder", "NCardRewardSelectionScreen" }, CardDisplayContext.Reward)]
    [InlineData(new[] { "NMerchantCard", "NMerchantInventory" }, CardDisplayContext.Shop)]
    [InlineData(new[] { "NCardHolder", "NCardPileScreen" }, CardDisplayContext.PileView)]
    [InlineData(new[] { "NCardHolder", "NPlayerHand" }, CardDisplayContext.Other)]
    public void Classify_MapsAncestorTypes(string[] ancestors, CardDisplayContext expected)
    {
        Assert.Equal(expected, CardContextClassifier.Classify(ancestors));
    }

    [Fact]
    public void Classify_EmptyAncestors_IsOther()
    {
        Assert.Equal(CardDisplayContext.Other, CardContextClassifier.Classify(Array.Empty<string>()));
    }

    [Fact]
    public void Classify_NotInPlayerPile_NoKnownAncestor_IsAcquisition()
    {
        Assert.Equal(
            CardDisplayContext.EventAcquisition,
            CardContextClassifier.Classify(Array.Empty<string>(), cardInPlayerPile: false));
        Assert.Equal(
            CardDisplayContext.EventAcquisition,
            CardContextClassifier.Classify(new[] { "NCardHolder", "NChooseACardSelectionScreen" }, cardInPlayerPile: false));
    }

    [Fact]
    public void Classify_ForceAcquisition_WinsOverOwnedView()
    {
        Assert.Equal(
            CardDisplayContext.EventAcquisition,
            CardContextClassifier.Classify(
                Array.Empty<string>(),
                cardInPlayerPile: false,
                cardInOwnedView: true,
                forceAcquisition: true));
    }

    [Fact]
    public void Classify_OwnedViewClone_NoPile_StaysOther()
    {
        Assert.Equal(
            CardDisplayContext.Other,
            CardContextClassifier.Classify(Array.Empty<string>(), cardInPlayerPile: false, cardInOwnedView: true));
    }

    [Fact]
    public void Classify_ExplicitAncestorContext_WinsOverPileState()
    {
        Assert.Equal(
            CardDisplayContext.Reward,
            CardContextClassifier.Classify(new[] { "NCardRewardSelectionScreen" }, cardInPlayerPile: false));
        Assert.Equal(
            CardDisplayContext.CardLibrary,
            CardContextClassifier.Classify(new[] { "NCardLibrary" }, cardInPlayerPile: false));
    }
}

public class MaskEligibilityTests
{
    [Theory]
    [InlineData(false, false, false, true)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(true, false, false, false)]
    public void ShouldMask_OnlyEnemyCreatures(bool disabled, bool isPlayer, bool isOwnPet, bool expected)
    {
        Assert.Equal(expected, MaskEligibility.ShouldMask(disabled, isPlayer, isOwnPet));
    }
}
