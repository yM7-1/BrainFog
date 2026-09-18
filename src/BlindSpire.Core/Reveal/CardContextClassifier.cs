using BlindSpire.Core.Reveal;

namespace BlindSpire.Core.Reveal;

/// <summary>
/// Classifies a card's display context from its ancestor node type names
/// (game-side passes the actual node type names, walking up the tree).
/// </summary>
public static class CardContextClassifier
{
    public static CardDisplayContext Classify(IEnumerable<string> ancestorTypeNames)
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
        return CardDisplayContext.Other;
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
