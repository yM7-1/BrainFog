using System.Runtime.CompilerServices;
using BrainFog.Core.Reveal;
using MegaCrit.Sts2.Core.Models;

namespace BrainFog.Game;

/// <summary>
/// Maps live CardModel instances to BrainFog instance ids. Used when the
/// "reveal same-name cards" difficulty option is off: only the exact played
/// copy is revealed. Ids are restored from the run save by deck order on load.
///
/// Combat clones identity (bug fix 2026-09-20): the game clones every deck card
/// into the draw pile at combat start (Player.PopulateCombatState) and the clone
/// carries DeckVersion pointing back at the run-level deck card. Identity must
/// follow the deck card, otherwise each combat's fresh clones mint new ids and
/// the reveal would reset every combat.
/// </summary>
internal static class CardInstanceRegistry
{
    private sealed class Holder
    {
        public string Id = InstanceIds.NewId();
    }

    private static readonly ConditionalWeakTable<CardModel, Holder> Map = new();

    /// <summary>The card whose identity this one shares: combat clones defer to
    /// their run-level deck card (DeckVersion), effect clones to their source
    /// (CloneOf); everything else is itself.</summary>
    private static CardModel IdentityOwner(CardModel card) =>
        CardIdentity.Resolve(card, c => c.DeckVersion ?? c.CloneOf);

    /// <summary>Existing id for a card, or null when it was never registered.</summary>
    public static string? TryGetId(CardModel card) =>
        Map.TryGetValue(IdentityOwner(card), out var holder) ? holder.Id : null;

    public static string GetOrCreateId(CardModel card) =>
        PatchGuard.RunOr("Registry.GetOrCreateId", () => GetOrCreateIdCore(IdentityOwner(card)), string.Empty);

    private static string GetOrCreateIdCore(CardModel card)
    {
        if (Map.TryGetValue(card, out var existing))
        {
            return existing.Id;
        }

        if (RevealPersistence.TryBindFromSave(card))
        {
            return Map.GetOrCreateValue(card).Id;
        }

        var id = InstanceIds.NewId();
        Map.Add(card, new Holder { Id = id });
        if (card.Owner is { } owner)
        {
            RevealPersistence.RefreshDeckOrder(owner);
        }
        return id;
    }

    public static void Bind(CardModel card, string id)
    {
        card = IdentityOwner(card);
        Map.Remove(card);
        Map.Add(card, new Holder { Id = id });
    }

    /// <summary>Used while refreshing the saved deck order; never triggers a refresh itself.</summary>
    public static string PeekOrBindNew(CardModel card)
    {
        card = IdentityOwner(card);
        if (Map.TryGetValue(card, out var existing))
        {
            return existing.Id;
        }

        var id = InstanceIds.NewId();
        Map.Add(card, new Holder { Id = id });
        return id;
    }
}
