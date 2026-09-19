namespace BrainFog.Core.Status;

/// <summary>
/// Snapshot of HP/gold shown to the player (spec 0.02 #3, 0.03 c/d).
/// Displayed values are stale until an explicit refresh trigger fires.
/// </summary>
public sealed class StatusSnapshot
{
    public int? Hp { get; private set; }

    public int? MaxHp { get; private set; }

    public int? Gold { get; private set; }

    public bool IsInitialized => Hp.HasValue && Gold.HasValue;

    public void Init(int hp, int maxHp, int gold)
    {
        Hp = hp;
        MaxHp = maxHp;
        Gold = gold;
    }

    public void RefreshHp(int hp, int maxHp)
    {
        Hp = hp;
        MaxHp = maxHp;
    }

    public void RefreshGold(int gold) => Gold = gold;

    /// <summary>Gold refreshes only on a deduction (shop/event spending, spec 0.02 #3).</summary>
    public bool ShouldRefreshGold(int newGold) => Gold is { } shown && newGold < shown;
}
