namespace BlindSpire.Core.Reveal;

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

    /// <summary>Event that grants a card (acquisition).</summary>
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

    /// <summary>Full true face.</summary>
    FullFace,
}

/// <summary>
/// Pure visibility rules (spec 0.02 #1/#2/#5, 0.03 b/g/h/j).
/// UI layers call this instead of duplicating policy.
/// </summary>
public static class RevealRules
{
    public static bool IsAcquisition(CardDisplayContext context) =>
        context is CardDisplayContext.Reward
            or CardDisplayContext.Shop
            or CardDisplayContext.EventAcquisition;

    public static CardVisualRule Resolve(CardDisplayContext context, CardKnowledge knowledge)
    {
        // Acquisition always shows rarity only, even for known instances (0.02 #5).
        if (IsAcquisition(context))
        {
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

    /// <summary>Unknown cards reveal only their frame on hover/selection (0.03 b).</summary>
    public static bool ShowsFrameOnHover(CardKnowledge knowledge) => true;

    /// <summary>The true face is shown on hover only once revealed.</summary>
    public static bool ShowsFaceOnHover(CardKnowledge knowledge) =>
        knowledge == CardKnowledge.Revealed;

    /// <summary>Upgraded cards show the "+" marker on acquisition (0.03 h).</summary>
    public static bool ShowsUpgradeMarker(bool isUpgraded) => isUpgraded;
}
