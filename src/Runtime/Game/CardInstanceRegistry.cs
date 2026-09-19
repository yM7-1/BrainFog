using System.Runtime.CompilerServices;
using BrainFog.Core.Reveal;
using MegaCrit.Sts2.Core.Models;

namespace BrainFog.Game;

/// <summary>
/// Maps live CardModel instances to BrainFog instance ids. Used when the
/// "reveal same-name cards" difficulty option is off: only the exact played
/// copy is revealed. Ids are restored from the run save by deck order on load.
/// </summary>
internal static class CardInstanceRegistry
{
    private sealed class Holder
    {
        public string Id = InstanceIds.NewId();
    }

    private static readonly ConditionalWeakTable<CardModel, Holder> Map = new();

    /// <summary>Existing id for a card, or null when it was never registered.</summary>
    public static string? TryGetId(CardModel card) =>
        Map.TryGetValue(card, out var holder) ? holder.Id : null;

    public static string GetOrCreateId(CardModel card) =>
        PatchGuard.RunOr("Registry.GetOrCreateId", () => GetOrCreateIdCore(card), string.Empty);

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
        Map.Remove(card);
        Map.Add(card, new Holder { Id = id });
    }

    /// <summary>Used while refreshing the saved deck order; never triggers a refresh itself.</summary>
    public static string PeekOrBindNew(CardModel card)
    {
        if (Map.TryGetValue(card, out var existing))
        {
            return existing.Id;
        }

        var id = InstanceIds.NewId();
        Map.Add(card, new Holder { Id = id });
        return id;
    }
}
