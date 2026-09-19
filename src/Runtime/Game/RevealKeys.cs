using MegaCrit.Sts2.Core.Models;

namespace BrainFog.Game;

/// <summary>
/// Stable per-run identity of a card definition (e.g. "cards.STRIKE").
/// Base and upgraded copies of the same card share the same key, so revealing
/// one copy reveals them all for the run (user change 2026-09-19).
/// </summary>
internal static class RevealKeys
{
    public static string Of(CardModel card) => card.Id.ToString();
}
