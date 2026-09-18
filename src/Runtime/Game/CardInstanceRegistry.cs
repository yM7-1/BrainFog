using System.Runtime.CompilerServices;
using BlindSpire.Core.Reveal;
using MegaCrit.Sts2.Core.Models;

namespace BlindSpire.Game;

/// <summary>
/// Maps live CardModel instances to BlindSpire instance ids.
/// Ids are restored from the run save by deck order on load (see RevealPersistence).
/// </summary>
internal static class CardInstanceRegistry
{
    private sealed class Holder
    {
        public string Id = InstanceIds.NewId();
    }

    private static readonly ConditionalWeakTable<CardModel, Holder> Map = new();

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
