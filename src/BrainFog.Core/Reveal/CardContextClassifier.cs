using BrainFog.Core.Reveal;

namespace BrainFog.Core.Reveal;

/// <summary>
/// Classifies a card's display context from its ancestor node type names
/// (game-side passes the actual node type names, walking up the tree).
/// Cards that sit in no player pile are being offered/acquired (event card
/// choices, generic card-select screens); with no explicit ancestor they map to
/// the acquisition rule so their face stays hidden (leak fix 2026-09-19).
/// </summary>
public static class CardContextClassifier
{
    public static CardDisplayContext Classify(
        IEnumerable<string> ancestorTypeNames,
        bool cardInPlayerPile = true,
        bool cardInOwnedView = false,
        bool forceAcquisition = false)
    {
        foreach (var name in ancestorTypeNames)
        {
            switch (name)
            {
                case "NCardLibrary":
                    return CardDisplayContext.CardLibrary;
                case "NCardRewardSelectionScreen":
                    return CardDisplayContext.Reward;
                case "NMerchantInventory":
                    return CardDisplayContext.Shop;
                case "NCardPileScreen":
                    return CardDisplayContext.PileView;
            }
        }
        // Cards opened from an acquisition screen (reward inspect view) are
        // acquisition even when the definition is already known.
        if (forceAcquisition)
        {
            return CardDisplayContext.EventAcquisition;
        }
        // Card-view screens (inspect/upgrade preview) display clones of owned
        // cards that live in no pile; they follow normal knowledge rules.
        if (cardInPlayerPile || cardInOwnedView)
        {
            return CardDisplayContext.Other;
        }
        return CardDisplayContext.EventAcquisition;
    }
}

/// <summary>
/// Pure eligibility for the enemy visual mask (spec 0.01 4.1).
/// </summary>
public static class MaskEligibility
{
    public static bool ShouldMask(bool modDisabled, bool isPlayer, bool isOwnPet) =>
        !modDisabled && !isPlayer && !isOwnPet;
}
