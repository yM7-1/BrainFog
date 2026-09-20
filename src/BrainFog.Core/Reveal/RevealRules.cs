using BrainFog.Core.Options;

namespace BrainFog.Core.Reveal;

/// <summary>Where a card face is being rendered; drives which visibility rule applies.</summary>
public enum CardDisplayContext
{
    /// <summary>Combat hand.</summary>
    Hand,

    /// <summary>Deck viewer out of combat.</summary>
    DeckView,

    DrawPile,
    DiscardPile,
    ExhaustPile,

    /// <summary>Generic pile view screen (draw/discard/exhaust share one screen class).</summary>
    PileView,

    /// <summary>Card reward screen (acquisition).</summary>
    Reward,

    /// <summary>Shop card stock (acquisition).</summary>
    Shop,

    /// <summary>Event that grants a card, or a generic acquisition selection screen.</summary>
    EventAcquisition,

    /// <summary>In-game card compendium (spec 0.03 j: never fogged).</summary>
    CardLibrary,

    Other,
}

/// <summary>What the player is allowed to see for a given card face.</summary>
public enum CardVisualRule
{
    /// <summary>Whole face is black fog.</summary>
    BlackFog,

    /// <summary>Only the rarity border (and upgrade "+" marker) is visible.</summary>
    RarityOnly,

    /// <summary>Full true face (card text is still garbled separately).</summary>
    FullFace,
}

/// <summary>
/// Pure visibility rules (spec 0.02 #1/#2/#5, 0.03 b/g/h/j) plus the player-facing
/// difficulty options (selection reveal count, shop/event reveal).
/// </summary>
public static class RevealRules
{
    public static bool IsAcquisition(CardDisplayContext context) =>
        context is CardDisplayContext.Reward
            or CardDisplayContext.Shop
            or CardDisplayContext.EventAcquisition;

    public static CardVisualRule Resolve(
        CardDisplayContext context,
        CardKnowledge knowledge,
        DifficultySettings? settings = null,
        bool selectionSlotRevealed = false)
    {
        // "Reveal all cards this run" wins over every masking rule (2026-09-21).
        if (settings?.RevealAllCards == true)
        {
            return CardVisualRule.FullFace;
        }

        // Acquisition shows rarity only by default (0.02 #5); difficulty options
        // can reveal the face (reward slot picked by SelectionRevealPlanner).
        if (IsAcquisition(context))
        {
            if (settings != null)
            {
                if (context == CardDisplayContext.Reward && selectionSlotRevealed)
                {
                    return CardVisualRule.FullFace;
                }
                if (context is CardDisplayContext.Shop or CardDisplayContext.EventAcquisition
                    && settings.RevealShopAndEventCards)
                {
                    return CardVisualRule.FullFace;
                }
            }
            return CardVisualRule.RarityOnly;
        }

        // The compendium is never fogged (0.03 j).
        if (context == CardDisplayContext.CardLibrary)
        {
            return CardVisualRule.FullFace;
        }

        return knowledge == CardKnowledge.Revealed
            ? CardVisualRule.FullFace
            : CardVisualRule.BlackFog;
    }

    /// <summary>Upgraded cards show the "+" marker on acquisition (0.03 h).</summary>
    public static bool ShowsUpgradeMarker(bool isUpgraded) => isUpgraded;
}
