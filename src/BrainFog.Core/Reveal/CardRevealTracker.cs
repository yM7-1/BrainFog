using System;
using System.Collections.Generic;

namespace BrainFog.Core.Reveal;

/// <summary>
/// Per-run knowledge state. Two scopes coexist (user change 2026-09-20):
/// - by card definition (default): any copy played/upgraded reveals the whole
///   card for the run (key like "cards.STRIKE", base and upgraded copies share it);
/// - by instance (difficulty option "reveal same-name cards" off): only the
///   played copy is revealed, tracked by a BrainFog-owned instance id.
/// </summary>
public sealed class CardRevealTracker
{
    private readonly HashSet<string> _revealed = new(StringComparer.Ordinal);
    private readonly HashSet<string> _revealedInstances = new(StringComparer.Ordinal);

    public int RevealedCount => _revealed.Count + _revealedInstances.Count;

    public IReadOnlyCollection<string> RevealedKeys => _revealed.ToArray();

    public IReadOnlyCollection<string> RevealedInstanceIds => _revealedInstances.ToArray();

    public CardKnowledge GetKnowledge(string definitionKey, string? instanceId = null) =>
        _revealed.Contains(definitionKey)
        || (!string.IsNullOrEmpty(instanceId) && _revealedInstances.Contains(instanceId))
            ? CardKnowledge.Revealed
            : CardKnowledge.Unknown;

    /// <summary>Definition-scope knowledge ("good memory" mode).</summary>
    public CardKnowledge GetDefinitionKnowledge(string definitionKey) =>
        _revealed.Contains(definitionKey) ? CardKnowledge.Revealed : CardKnowledge.Unknown;

    /// <summary>Per-copy knowledge ("bad memory" mode).</summary>
    public CardKnowledge GetInstanceKnowledge(string? instanceId) =>
        !string.IsNullOrEmpty(instanceId) && _revealedInstances.Contains(instanceId)
            ? CardKnowledge.Revealed
            : CardKnowledge.Unknown;

    public bool IsRevealed(string definitionKey) => _revealed.Contains(definitionKey);

    public bool IsInstanceRevealed(string instanceId) => _revealedInstances.Contains(instanceId);

    /// <summary>Definition-scope reveal (default difficulty).</summary>
    public bool RevealByPlay(string definitionKey) => _revealed.Add(definitionKey);

    /// <summary>Instance-scope reveal (same-name reveal disabled).</summary>
    public bool RevealInstanceByPlay(string instanceId) => _revealedInstances.Add(instanceId);

    /// <summary>Upgrades reveal the true face even if never played (0.03 a).</summary>
    public bool RevealByUpgrade(string definitionKey) => _revealed.Add(definitionKey);

    public bool RevealInstanceByUpgrade(string instanceId) => _revealedInstances.Add(instanceId);

    /// <summary>"Bad memory": hides a copy again (its counter reached n).</summary>
    public bool HideInstance(string instanceId) => _revealedInstances.Remove(instanceId);

    /// <summary>Restores persisted state (per-run scope).</summary>
    public void Load(IEnumerable<string>? definitionKeys) => Load(definitionKeys, null);

    public void Load(IEnumerable<string>? definitionKeys, IEnumerable<string>? instanceIds)
    {
        _revealed.Clear();
        _revealedInstances.Clear();
        if (definitionKeys != null)
        {
            foreach (var key in definitionKeys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    _revealed.Add(key);
                }
            }
        }
        if (instanceIds != null)
        {
            foreach (var id in instanceIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    _revealedInstances.Add(id);
                }
            }
        }
    }

    /// <summary>New run: everything returns to black fog.</summary>
    public void Reset()
    {
        _revealed.Clear();
        _revealedInstances.Clear();
    }
}
