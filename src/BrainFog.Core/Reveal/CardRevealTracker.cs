using System;
using System.Collections.Generic;

namespace BrainFog.Core.Reveal;

/// <summary>
/// Per-run knowledge state keyed by card definition (user change 2026-09-19):
/// once any copy of a card has been played (or upgraded), every copy of that
/// card — including copies acquired later — shows its true face for the rest
/// of the run. The key is the model id string (e.g. "cards.STRIKE"), so base
/// and upgraded copies of the same card share knowledge.
/// </summary>
public sealed class CardRevealTracker
{
    private readonly HashSet<string> _revealed = new(StringComparer.Ordinal);

    public int RevealedCount => _revealed.Count;

    public IReadOnlyCollection<string> RevealedKeys => _revealed.ToArray();

    public CardKnowledge GetKnowledge(string definitionKey) =>
        _revealed.Contains(definitionKey) ? CardKnowledge.Revealed : CardKnowledge.Unknown;

    public bool IsRevealed(string definitionKey) => _revealed.Contains(definitionKey);

    /// <summary>Playing any copy reveals the whole card definition for the run.</summary>
    public bool RevealByPlay(string definitionKey) => _revealed.Add(definitionKey);

    /// <summary>Upgrading reveals the definition even if never played (0.03 a).</summary>
    public bool RevealByUpgrade(string definitionKey) => _revealed.Add(definitionKey);

    /// <summary>Restores persisted state (per-run scope).</summary>
    public void Load(IEnumerable<string>? definitionKeys)
    {
        _revealed.Clear();
        if (definitionKeys == null)
        {
            return;
        }
        foreach (var key in definitionKeys)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _revealed.Add(key);
            }
        }
    }

    /// <summary>New run: everything returns to black fog.</summary>
    public void Reset() => _revealed.Clear();
}
