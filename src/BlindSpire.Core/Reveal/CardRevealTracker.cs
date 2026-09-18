using System;
using System.Collections.Generic;

namespace BlindSpire.Core.Reveal;

/// <summary>
/// Per-run knowledge state for card instances (spec 0.02 #6 / 0.03 a,g).
/// Per-instance, not per-definition: two copies of the same card are tracked separately.
/// </summary>
public sealed class CardRevealTracker
{
    private readonly HashSet<string> _revealed = new(StringComparer.Ordinal);

    public int RevealedCount => _revealed.Count;

    public IReadOnlyCollection<string> RevealedIds => _revealed;

    public CardKnowledge GetKnowledge(string instanceId) =>
        _revealed.Contains(instanceId) ? CardKnowledge.Revealed : CardKnowledge.Unknown;

    public bool IsRevealed(string instanceId) => _revealed.Contains(instanceId);

    /// <summary>Reveals the instance after it has been played once (0.02 #6).</summary>
    public bool RevealByPlay(string instanceId) => _revealed.Add(instanceId);

    /// <summary>Upgrades reveal the true face even if never played (0.03 a).</summary>
    public bool RevealByUpgrade(string instanceId) => _revealed.Add(instanceId);

    /// <summary>Restores persisted state (per-run scope).</summary>
    public void Load(IEnumerable<string> instanceIds)
    {
        _revealed.Clear();
        foreach (var id in instanceIds)
        {
            if (InstanceIds.IsValid(id))
            {
                _revealed.Add(id);
            }
        }
    }

    /// <summary>New run: everything returns to black fog.</summary>
    public void Reset() => _revealed.Clear();
}
